// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/EndpointRoutingMiddleware.cs
// Source Sha256: 651e450043a2715056b5063c984746237c3ec047

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;



using Pipeware;
using Pipeware.Features;
using Pipeware.Metadata;
using Pipeware.Routing.Matching;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Pipeware.Routing;

internal sealed partial class EndpointRoutingMiddleware<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]TRequestContext> where TRequestContext : class, IRequestContext
{
    private const string DiagnosticsEndpointMatchedKey = "Microsoft.AspNetCore.Routing.EndpointMatched";

    private readonly MatcherFactory<TRequestContext> _matcherFactory;
    private readonly ILogger _logger;
    private readonly EndpointDataSource<TRequestContext> _endpointDataSource;
    private readonly DiagnosticListener _diagnosticListener;
    private readonly RoutingMetrics _metrics;
    private readonly RequestDelegate<TRequestContext> _next;
    private readonly RouteOptions<TRequestContext> _routeOptions;
    private Task<Matcher<TRequestContext>>? _initializationTask;

    public EndpointRoutingMiddleware(
        MatcherFactory<TRequestContext> matcherFactory,
        ILogger<EndpointRoutingMiddleware<TRequestContext>> logger,
        IEndpointRouteBuilder<TRequestContext> endpointRouteBuilder,
        EndpointDataSource<TRequestContext> rootCompositeEndpointDataSource,
        DiagnosticListener diagnosticListener,
        IOptions<RouteOptions<TRequestContext>> routeOptions,
        RoutingMetrics metrics,
        RequestDelegate<TRequestContext> next)
    {
        ArgumentNullException.ThrowIfNull(endpointRouteBuilder);

        _matcherFactory = matcherFactory ?? throw new ArgumentNullException(nameof(matcherFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _diagnosticListener = diagnosticListener ?? throw new ArgumentNullException(nameof(diagnosticListener));
        _metrics = metrics;
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _routeOptions = routeOptions.Value;

        // rootCompositeEndpointDataSource is a constructor parameter only so it always gets disposed by DI. This ensures that any
        // disposable EndpointDataSources also get disposed. _endpointDataSource is a component of rootCompositeEndpointDataSource.
        _ = rootCompositeEndpointDataSource;
        _endpointDataSource = new CompositeEndpointDataSource<TRequestContext>(endpointRouteBuilder.DataSources);
    }

    public Task Invoke(TRequestContext requestContext)
    {
        // There's already an endpoint, skip matching completely
        var endpoint = requestContext.GetEndpoint();
        if (endpoint != null)
        {
            Log.MatchSkipped(_logger, endpoint);
            return _next(requestContext);
        }

        // There's an inherent race condition between waiting for init and accessing the matcher
        // this is OK because once `_matcher` is initialized, it will not be set to null again.
        var matcherTask = InitializeAsync();
        if (!matcherTask.IsCompletedSuccessfully)
        {
            return AwaitMatcher(this, requestContext, matcherTask);
        }

        var matchTask = matcherTask.Result.MatchAsync(requestContext);
        if (!matchTask.IsCompletedSuccessfully)
        {
            return AwaitMatch(this, requestContext, matchTask);
        }

        return SetRoutingAndContinue(requestContext);

        // Awaited fallbacks for when the Tasks do not synchronously complete
        static async Task AwaitMatcher(EndpointRoutingMiddleware<TRequestContext> middleware, TRequestContext requestContext, Task<Matcher<TRequestContext>> matcherTask)
        {
            var matcher = await matcherTask;
            await matcher.MatchAsync(requestContext);
            await middleware.SetRoutingAndContinue(requestContext);
        }

        static async Task AwaitMatch(EndpointRoutingMiddleware<TRequestContext> middleware, TRequestContext requestContext, Task matchTask)
        {
            await matchTask;
            await middleware.SetRoutingAndContinue(requestContext);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Task SetRoutingAndContinue(TRequestContext requestContext)
    {
        // If there was no mutation of the endpoint then log failure
        var endpoint = requestContext.GetEndpoint();
        if (endpoint == null)
        {
            Log.MatchFailure(_logger);
            _metrics.MatchFailure();
        }
        else
        {
            // Raise an event if the route matched
            if (_diagnosticListener.IsEnabled() && _diagnosticListener.IsEnabled(DiagnosticsEndpointMatchedKey))
            {
                Write(_diagnosticListener, requestContext);
            }

            if (_logger.IsEnabled(LogLevel.Debug) || _metrics.MatchSuccessCounterEnabled)
            {
                var isFallback = endpoint.Metadata.GetMetadata<FallbackMetadata>() is not null;

                Log.MatchSuccess(_logger, endpoint);

                if (isFallback)
                {
                    Log.FallbackMatch(_logger, endpoint);
                }

                // It shouldn't be possible for a route to be matched via the route matcher and not have a route.
                // Just in case, add a special (missing) value as the route tag to metrics.
                var route = endpoint.Metadata.GetMetadata<IRouteDiagnosticsMetadata>()?.Route ?? "(missing)";
                _metrics.MatchSuccess(route, isFallback);
            }
        }

        return _next(requestContext);

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
            Justification = "The values being passed into Write are being consumed by the application already.")]
        static void Write(DiagnosticListener diagnosticListener, TRequestContext requestContext)
        {
            // We're just going to send the HttpContext since it has all of the relevant information
            diagnosticListener.Write(DiagnosticsEndpointMatchedKey, requestContext);
        }
    }

    // Initialization is async to avoid blocking threads while reflection and things
    // of that nature take place.
    //
    // We've seen cases where startup is very slow if we  allow multiple threads to race
    // while initializing the set of endpoints/routes. Doing CPU intensive work is a
    // blocking operation if you have a low core count and enough work to do.
    private Task<Matcher<TRequestContext>> InitializeAsync()
    {
        var initializationTask = _initializationTask;
        if (initializationTask != null)
        {
            return initializationTask;
        }

        return InitializeCoreAsync();
    }

    private Task<Matcher<TRequestContext>> InitializeCoreAsync()
    {
        var initialization = new TaskCompletionSource<Matcher<TRequestContext>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var initializationTask = Interlocked.CompareExchange(ref _initializationTask, initialization.Task, null);
        if (initializationTask != null)
        {
            // This thread lost the race, join the existing task.
            return initializationTask;
        }

        // This thread won the race, do the initialization.
        try
        {
            var matcher = _matcherFactory.CreateMatcher(_endpointDataSource);

            _initializationTask = Task.FromResult(matcher);

            // Complete the task, this will unblock any requests that came in while initializing.
            initialization.SetResult(matcher);
            return initialization.Task;
        }
        catch (Exception ex)
        {
            // Allow initialization to occur again. Since DataSources can change, it's possible
            // for the developer to correct the data causing the failure.
            _initializationTask = null;

            // Complete the task, this will throw for any requests that came in while initializing.
            initialization.SetException(ex);
            return initialization.Task;
        }
    }

    private static partial class Log
    {
        public static void MatchSuccess(ILogger logger, Endpoint<TRequestContext> endpoint)
            => MatchSuccess(logger, endpoint.DisplayName);

        [LoggerMessage(1, LogLevel.Debug, "Request matched endpoint '{EndpointName}'", EventName = "MatchSuccess")]
        private static partial void MatchSuccess(ILogger logger, string? endpointName);

        [LoggerMessage(2, LogLevel.Debug, "Request did not match any endpoints", EventName = "MatchFailure")]
        public static partial void MatchFailure(ILogger logger);

        public static void MatchSkipped(ILogger logger, Endpoint<TRequestContext> endpoint)
            => MatchingSkipped(logger, endpoint.DisplayName);

        [LoggerMessage(3, LogLevel.Debug, "Endpoint '{EndpointName}' already set, skipping route matching.", EventName = "MatchingSkipped")]
        private static partial void MatchingSkipped(ILogger logger, string? endpointName);

        [LoggerMessage(4, LogLevel.Information, "The endpoint '{EndpointName}' is being executed without running additional middleware.", EventName = "ExecutingEndpoint")]
        public static partial void ExecutingEndpoint(ILogger logger, Endpoint<TRequestContext> endpointName);

        [LoggerMessage(5, LogLevel.Information, "The endpoint '{EndpointName}' has been executed without running additional middleware.", EventName = "ExecutedEndpoint")]
        public static partial void ExecutedEndpoint(ILogger logger, Endpoint<TRequestContext> endpointName);

        [LoggerMessage(6, LogLevel.Information, "The endpoint '{EndpointName}' is being short circuited without running additional middleware or producing a response.", EventName = "ShortCircuitedEndpoint")]
        public static partial void ShortCircuitedEndpoint(ILogger logger, Endpoint<TRequestContext> endpointName);

        [LoggerMessage(7, LogLevel.Debug, "Matched endpoint '{EndpointName}' is a fallback endpoint.", EventName = "FallbackMatch")]
        public static partial void FallbackMatch(ILogger logger, Endpoint<TRequestContext> endpointName);
    }
}