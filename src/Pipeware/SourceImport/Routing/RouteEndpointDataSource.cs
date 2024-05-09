// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/RouteEndpointDataSource.cs
// Source Sha256: af40b6c1757a2ae2f5d66284963a30092f8b4b0a

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Pipeware.Builder;
using Pipeware;
using Pipeware.Routing.Patterns;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;
using Pipeware.Internal;

namespace Pipeware.Routing;

internal sealed class RouteEndpointDataSource<TRequestContext> : EndpointDataSource<TRequestContext> where TRequestContext : class, IRequestContext
{
    private readonly List<RouteEntry> _routeEntries = new();
    private readonly IServiceProvider _applicationServices;
    private readonly bool _throwOnBadRequest;

    public RouteEndpointDataSource(IServiceProvider applicationServices, bool throwOnBadRequest)
    {
        _applicationServices = applicationServices;
        _throwOnBadRequest = throwOnBadRequest;
    }

    public RouteHandlerBuilder<TRequestContext> AddRequestDelegate(
        RoutePattern pattern,
        RequestDelegate<TRequestContext> requestDelegate,
        Func<Delegate, RequestDelegateFactoryOptions<TRequestContext>, RequestDelegateMetadataResult?, RequestDelegateResult<TRequestContext>> createHandlerRequestDelegateFunc)
    {
        var conventions = new ThrowOnAddAfterEndpointBuiltConventionCollection();
        var finallyConventions = new ThrowOnAddAfterEndpointBuiltConventionCollection();

        _routeEntries.Add(new()
        {
            RoutePattern = pattern,
            RouteHandler = requestDelegate,
            RouteAttributes = RouteAttributes.None,
            Conventions = conventions,
            FinallyConventions = finallyConventions,
            InferMetadataFunc = null, // Metadata isn't infered from RequestDelegate endpoints
            CreateHandlerRequestDelegateFunc = createHandlerRequestDelegateFunc
        });

        return new RouteHandlerBuilder<TRequestContext>(conventions, finallyConventions);
    }

    public RouteHandlerBuilder<TRequestContext> AddRouteHandler(
        RoutePattern pattern,
        Delegate routeHandler,
        bool isFallback,
        Func<MethodInfo, RequestDelegateFactoryOptions<TRequestContext>?, RequestDelegateMetadataResult>? inferMetadataFunc,
        Func<Delegate, RequestDelegateFactoryOptions<TRequestContext>, RequestDelegateMetadataResult?, RequestDelegateResult<TRequestContext>> createHandlerRequestDelegateFunc)
    {
        var conventions = new ThrowOnAddAfterEndpointBuiltConventionCollection();
        var finallyConventions = new ThrowOnAddAfterEndpointBuiltConventionCollection();

        var routeAttributes = RouteAttributes.RouteHandler;
        if (isFallback)
        {
            routeAttributes |= RouteAttributes.Fallback;
        }

        _routeEntries.Add(new()
        {
            RoutePattern = pattern,
            RouteHandler = routeHandler,
            RouteAttributes = routeAttributes,
            Conventions = conventions,
            FinallyConventions = finallyConventions,
            InferMetadataFunc = inferMetadataFunc,
            CreateHandlerRequestDelegateFunc = createHandlerRequestDelegateFunc
        });

        return new RouteHandlerBuilder<TRequestContext>(conventions, finallyConventions);
    }

    public override IReadOnlyList<RouteEndpoint<TRequestContext>> Endpoints
    {
        get
        {
            var endpoints = new RouteEndpoint<TRequestContext>[_routeEntries.Count];
            for (int i = 0; i < _routeEntries.Count; i++)
            {
                endpoints[i] = (RouteEndpoint<TRequestContext>)CreateRouteEndpointBuilder(_routeEntries[i]).Build();
            }
            return endpoints;
        }
    }

    public override IReadOnlyList<RouteEndpoint<TRequestContext>> GetGroupedEndpoints(RouteGroupContext<TRequestContext> context)
    {
        var endpoints = new RouteEndpoint<TRequestContext>[_routeEntries.Count];
        for (int i = 0; i < _routeEntries.Count; i++)
        {
            endpoints[i] = (RouteEndpoint<TRequestContext>)CreateRouteEndpointBuilder(_routeEntries[i], context.Prefix, context.Conventions, context.FinallyConventions).Build();
        }
        return endpoints;
    }

    public override IChangeToken GetChangeToken() => NullChangeToken.Singleton;

    // For testing
    internal RouteEndpointBuilder<TRequestContext> GetSingleRouteEndpointBuilder()
    {
        if (_routeEntries.Count is not 1)
        {
            throw new InvalidOperationException($"There are {_routeEntries.Count} endpoints defined! This can only be called for a single endpoint.");
        }

        return CreateRouteEndpointBuilder(_routeEntries[0]);
    }

    private RouteEndpointBuilder<TRequestContext> CreateRouteEndpointBuilder(
        RouteEntry entry, RoutePattern? groupPrefix = null, IReadOnlyList<Action<EndpointBuilder<TRequestContext>>>? groupConventions = null, IReadOnlyList<Action<EndpointBuilder<TRequestContext>>>? groupFinallyConventions = null)
    {
        var pattern = RoutePatternFactory.Combine(groupPrefix, entry.RoutePattern);
        var handler = entry.RouteHandler;
        var isRouteHandler = (entry.RouteAttributes & RouteAttributes.RouteHandler) == RouteAttributes.RouteHandler;
        var isFallback = (entry.RouteAttributes & RouteAttributes.Fallback) == RouteAttributes.Fallback;

        // The Map methods don't support customizing the order apart from using int.MaxValue to give MapFallback the lowest priority.
        // Otherwise, we always use the default of 0 unless a convention changes it later.
        var order = isFallback ? int.MaxValue : 0;
        var displayName = pattern.DebuggerToString();

        // Don't include the method name for non-route-handlers because the name is just "Invoke" when built from
        // ApplicationBuilder.Build(). This was observed in MapSignalRTests and is not very useful. Maybe if we come up
        // with a better heuristic for what a useful method name is, we could use it for everything. Inline lambdas are
        // compiler generated methods so they are filtered out even for route handlers.
        if (isRouteHandler && TypeHelper.TryGetNonCompilerGeneratedMethodName(handler.Method, out var methodName))
        {
            displayName = $"{displayName} => {methodName}";
        }

        if (isFallback)
        {
            displayName = $"Fallback {displayName}";
        }

        // If we're not a route handler, we started with a fully realized (although unfiltered) RequestDelegate, so we can just redirect to that
        // while running any conventions. We'll put the original back if it remains unfiltered right before building the endpoint.
        RequestDelegate<TRequestContext>? factoryCreatedRequestDelegate = isRouteHandler ? null : (RequestDelegate<TRequestContext>)entry.RouteHandler;

        // Let existing conventions capture and call into builder.RequestDelegate as long as they do so after it has been created.
        RequestDelegate<TRequestContext> redirectRequestDelegate = context =>
        {
            if (factoryCreatedRequestDelegate is null)
            {
                throw new InvalidOperationException("This RequestDelegate cannot be called before the final endpoint is built.");
            }

            return factoryCreatedRequestDelegate(context);
        };

        // Add MethodInfo and HttpMethodMetadata (if any) as first metadata items as they are intrinsic to the route much like
        // the pattern or default display name. This gives visibility to conventions like WithOpenApi() to intrinsic route details
        // (namely the MethodInfo) even when applied early as group conventions.
        RouteEndpointBuilder<TRequestContext> builder = new(redirectRequestDelegate, pattern, order)
        {
            DisplayName = displayName,
            ApplicationServices = _applicationServices,
        };

        if (isFallback)
        {
            builder.Metadata.Add(FallbackMetadata.Instance);
        }

        if (isRouteHandler)
        {
            builder.Metadata.Add(handler.Method);
        }

        // Apply group conventions before entry-specific conventions added to the RouteHandlerBuilder.
        if (groupConventions is not null)
        {
            foreach (var groupConvention in groupConventions)
            {
                groupConvention(builder);
            }
        }

        RequestDelegateFactoryOptions<TRequestContext>? rdfOptions = null;
        RequestDelegateMetadataResult? rdfMetadataResult = null;

        // Any metadata inferred directly inferred by RDF or indirectly inferred via IEndpoint(Parameter)MetadataProviders are
        // considered less specific than method-level attributes and conventions but more specific than group conventions
        // so inferred metadata gets added in between these. If group conventions need to override inferred metadata,
        // they can do so via IEndpointConventionBuilder.Finally like the do to override any other entry-specific metadata.
        if (isRouteHandler)
        {
            Debug.Assert(entry.InferMetadataFunc != null, "A func to infer metadata must be provided for route handlers.");

            rdfOptions = CreateRdfOptions(entry, pattern, builder);
            rdfMetadataResult = entry.InferMetadataFunc(entry.RouteHandler.Method, rdfOptions);
        }

        // Add delegate attributes as metadata before entry-specific conventions but after group conventions.
        var attributes = handler.Method.GetCustomAttributes();
        if (attributes is not null)
        {
            foreach (var attribute in attributes)
            {
                builder.Metadata.Add(attribute);
            }
        }

        entry.Conventions.IsReadOnly = true;
        foreach (var entrySpecificConvention in entry.Conventions)
        {
            entrySpecificConvention(builder);
        }

        // If no convention has modified builder.RequestDelegate, we can use the RequestDelegate returned by the RequestDelegateFactory directly.
        var conventionOverriddenRequestDelegate = ReferenceEquals(builder.RequestDelegate, redirectRequestDelegate) ? null : builder.RequestDelegate;

        if (isRouteHandler || builder.FilterFactories.Count > 0)
        {
            rdfOptions ??= CreateRdfOptions(entry, pattern, builder);

            // We ignore the returned EndpointMetadata has been already populated since we passed in non-null EndpointMetadata.
            // We always set factoryRequestDelegate in case something is still referencing the redirected version of the RequestDelegate.
            factoryCreatedRequestDelegate = entry.CreateHandlerRequestDelegateFunc(entry.RouteHandler, rdfOptions, rdfMetadataResult).RequestDelegate;
        }

        Debug.Assert(factoryCreatedRequestDelegate is not null);

        // Use the overridden RequestDelegate if it exists. If the overridden RequestDelegate is merely wrapping the final RequestDelegate,
        // it will still work because of the redirectRequestDelegate.
        builder.RequestDelegate = conventionOverriddenRequestDelegate ?? factoryCreatedRequestDelegate;

        entry.FinallyConventions.IsReadOnly = true;
        foreach (var entryFinallyConvention in entry.FinallyConventions)
        {
            entryFinallyConvention(builder);
        }

        if (groupFinallyConventions is not null)
        {
            // Group conventions are ordered by the RouteGroupBuilder before
            // being provided here.
            foreach (var groupFinallyConvention in groupFinallyConventions)
            {
                groupFinallyConvention(builder);
            }
        }

        return builder;
    }

    private RequestDelegateFactoryOptions<TRequestContext> CreateRdfOptions(RouteEntry entry, RoutePattern pattern, RouteEndpointBuilder<TRequestContext> builder)
    {
        return new()
        {
            ServiceProvider = _applicationServices,
            RouteParameterNames = ProduceRouteParamNames(),
            ThrowOnBadRequest = _throwOnBadRequest,
            EndpointBuilder = builder,
        };

        IEnumerable<string> ProduceRouteParamNames()
        {
            foreach (var routePatternPart in pattern.Parameters)
            {
                yield return routePatternPart.Name;
            }
        }
    }

    private readonly struct RouteEntry
    {
        public required RoutePattern RoutePattern { get; init; }
        public required Delegate RouteHandler { get; init; }
        public required RouteAttributes RouteAttributes { get; init; }
        public required ThrowOnAddAfterEndpointBuiltConventionCollection Conventions { get; init; }
        public required ThrowOnAddAfterEndpointBuiltConventionCollection FinallyConventions { get; init; }
        public required Func<MethodInfo, RequestDelegateFactoryOptions<TRequestContext>?, RequestDelegateMetadataResult>? InferMetadataFunc { get; init; }
        public required Func<Delegate, RequestDelegateFactoryOptions<TRequestContext>, RequestDelegateMetadataResult?, RequestDelegateResult<TRequestContext>> CreateHandlerRequestDelegateFunc { get; init; }
    }

    [Flags]
    private enum RouteAttributes
    {
        // The endpoint was defined by a RequestDelegate, RequestDelegateFactory.Create() should be skipped unless there are endpoint filters.
        None = 0,
        // This was added as Delegate route handler, so RequestDelegateFactory.Create() should always be called.
        RouteHandler = 1,
        // This was added by MapFallback.
        Fallback = 2,
    }

    // This private class is only exposed to internal code via ICollection<Action<EndpointBuilder>> in RouteEndpointBuilder where only Add is called.
    private sealed class ThrowOnAddAfterEndpointBuiltConventionCollection : List<Action<EndpointBuilder<TRequestContext>>>, ICollection<Action<EndpointBuilder<TRequestContext>>>
    {
        // We throw if someone tries to add conventions to the RouteEntry after endpoints have already been resolved meaning the conventions
        // will not be observed given RouteEndpointDataSource is not meant to be dynamic and uses NullChangeToken.Singleton.
        public bool IsReadOnly { get; set; }

        void ICollection<Action<EndpointBuilder<TRequestContext>>>.Add(Action<EndpointBuilder<TRequestContext>> convention)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException("Conventions cannot be added after building the endpoint.");
            }

            Add(convention);
        }
    }
}