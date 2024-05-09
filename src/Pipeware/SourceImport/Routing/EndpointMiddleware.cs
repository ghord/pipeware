// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/EndpointMiddleware.cs
// Source Sha256: 95f4abbba54f37d81c6ac7b119720f31a791d76d

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.




using Pipeware;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Pipeware.Routing;

internal sealed partial class EndpointMiddleware<TRequestContext> where TRequestContext : class, IRequestContext
{
    internal const string AuthorizationMiddlewareInvokedKey = "__AuthorizationMiddlewareWithEndpointInvoked";
    internal const string CorsMiddlewareInvokedKey = "__CorsMiddlewareWithEndpointInvoked";
    internal const string AntiforgeryMiddlewareWithEndpointInvokedKey = "__AntiforgeryMiddlewareWithEndpointInvoked";

    private readonly ILogger _logger;
    private readonly RequestDelegate<TRequestContext> _next;
    private readonly RouteOptions<TRequestContext> _routeOptions;

    public EndpointMiddleware(
        ILogger<EndpointMiddleware<TRequestContext>> logger,
        RequestDelegate<TRequestContext> next,
        IOptions<RouteOptions<TRequestContext>> routeOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _routeOptions = routeOptions?.Value ?? throw new ArgumentNullException(nameof(routeOptions));
    }

    public Task Invoke(TRequestContext requestContext)
    {
        var endpoint = requestContext.GetEndpoint();
        if (endpoint is not null)
        {

            if (endpoint.RequestDelegate is not null)
            {
                if (!_logger.IsEnabled(LogLevel.Information))
                {
                    // Avoid the AwaitRequestTask state machine allocation if logging is disabled.
                    return endpoint.RequestDelegate(requestContext);
                }

                Log.ExecutingEndpoint(_logger, endpoint);

                try
                {
                    var requestTask = endpoint.RequestDelegate(requestContext);
                    if (!requestTask.IsCompletedSuccessfully)
                    {
                        return AwaitRequestTask(endpoint, requestTask, _logger);
                    }
                }
                catch
                {
                    Log.ExecutedEndpoint(_logger, endpoint);
                    throw;
                }

                Log.ExecutedEndpoint(_logger, endpoint);
                return Task.CompletedTask;
            }
        }

        return _next(requestContext);

        static async Task AwaitRequestTask(Endpoint<TRequestContext> endpoint, Task requestTask, ILogger logger)
        {
            try
            {
                await requestTask;
            }
            finally
            {
                Log.ExecutedEndpoint(logger, endpoint);
            }
        }
    }

    private static void ThrowMissingAuthMiddlewareException(Endpoint<TRequestContext> endpoint)
    {
        throw new InvalidOperationException($"Endpoint {endpoint.DisplayName} contains authorization metadata, " +
            "but a middleware was not found that supports authorization." +
            Environment.NewLine +
            "Configure your application startup by adding app.UseAuthorization() in the application startup code. If there are calls to app.UseRouting() and app.UseEndpoints(...), the call to app.UseAuthorization() must go between them.");
    }

    private static void ThrowMissingCorsMiddlewareException(Endpoint<TRequestContext> endpoint)
    {
        throw new InvalidOperationException($"Endpoint {endpoint.DisplayName} contains CORS metadata, " +
            "but a middleware was not found that supports CORS." +
            Environment.NewLine +
            "Configure your application startup by adding app.UseCors() in the application startup code. If there are calls to app.UseRouting() and app.UseEndpoints(...), the call to app.UseCors() must go between them.");
    }

    private static void ThrowMissingAntiforgeryMiddlewareException(Endpoint<TRequestContext> endpoint)
    {
        throw new InvalidOperationException($"Endpoint {endpoint.DisplayName} contains anti-forgery metadata, " +
            "but a middleware was not found that supports anti-forgery." +
            Environment.NewLine +
            "Configure your application startup by adding app.UseAntiforgery() in the application startup code. If there are calls to app.UseRouting() and app.UseEndpoints(...), the call to app.UseAntiforgery() must go between them. " +
            "Calls to app.UseAntiforgery() must be placed after calls to app.UseAuthentication() and app.UseAuthorization().");
    }

    private static partial class Log
    {
        [LoggerMessage(0, LogLevel.Information, "Executing endpoint '{EndpointName}'", EventName = "ExecutingEndpoint")]
        public static partial void ExecutingEndpoint(ILogger logger, Endpoint<TRequestContext> endpointName);

        [LoggerMessage(1, LogLevel.Information, "Executed endpoint '{EndpointName}'", EventName = "ExecutedEndpoint")]
        public static partial void ExecutedEndpoint(ILogger logger, Endpoint<TRequestContext> endpointName);
    }
}