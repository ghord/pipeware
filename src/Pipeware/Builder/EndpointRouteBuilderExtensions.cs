// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source File: src/Http/Routing/src/Builder/EndpointRouteBuilderExtensions.cs

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Pipeware.Features;
using Pipeware.Internal;
using Pipeware.Routing;
using Pipeware.Routing.Patterns;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Pipeware.Builder;

public static partial class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches requests
    /// for the specified pattern.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
    public static RouteHandlerBuilder<TRequestContext> Map<TRequestContext>(
        this IEndpointRouteBuilder<TRequestContext> endpoints,
        RoutePattern pattern,
        RequestDelegate<TRequestContext> requestDelegate) where TRequestContext : class, IRequestContext
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(requestDelegate);

        return endpoints
            .GetOrAddRouteEndpointDataSource()
            .AddRequestDelegate(pattern, requestDelegate, CreateHandlerRequestDelegate);

        static RequestDelegateResult<TRequestContext> CreateHandlerRequestDelegate(Delegate handler, RequestDelegateFactoryOptions<TRequestContext> options, RequestDelegateMetadataResult? metadataResult)
        {
            var requestDelegate = (RequestDelegate<TRequestContext>)handler;

            // Create request delegate that calls filter pipeline.
            if (options.EndpointBuilder?.FilterFactories.Count > 0)
            {
                requestDelegate = CreateFilteredDelegate(requestDelegate, options);
            }

            IReadOnlyList<object> metadata = options.EndpointBuilder?.Metadata is not null ?
                new List<object>(options.EndpointBuilder.Metadata) :
                Array.Empty<object>();
            return new RequestDelegateResult<TRequestContext>(requestDelegate, metadata);
        }


    }

    /// <summary>
    /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP requests
    /// for the specified pattern.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="handler">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="RouteHandlerBuilder"/> that can be used to further customize the endpoint.</returns>
    [RequiresUnreferencedCode(MapEndpointUnreferencedCodeWarning)]
    [RequiresDynamicCode(MapEndpointDynamicCodeWarning)]
    public static RouteHandlerBuilder<TRequestContext> Map<TRequestContext>(
        this IEndpointRouteBuilder<TRequestContext> endpoints,
        RoutePattern pattern,
        Delegate handler) where TRequestContext : class, IRequestContext
    {
        return Map(endpoints, pattern, handler, isFallback: false);
    }


    [RequiresUnreferencedCode(MapEndpointUnreferencedCodeWarning)]
    [RequiresDynamicCode(MapEndpointDynamicCodeWarning)]
    private static RouteHandlerBuilder<TRequestContext> Map<TRequestContext>(
           this IEndpointRouteBuilder<TRequestContext> endpoints,
           RoutePattern pattern,
           Delegate handler,
           bool isFallback) where TRequestContext : class, IRequestContext
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(handler);

        return endpoints
            .GetOrAddRouteEndpointDataSource()
            .AddRouteHandler(pattern, handler, isFallback, RequestDelegateFactory<TRequestContext>.InferMetadata, RequestDelegateFactory<TRequestContext>.Create);
    }

    private static RequestDelegate<TRequestContext> CreateFilteredDelegate<TRequestContext>(RequestDelegate<TRequestContext> requestDelegate, RequestDelegateFactoryOptions<TRequestContext> options) where TRequestContext : class, IRequestContext
    {
        Debug.Assert(options.EndpointBuilder != null);

        var serviceProvider = options.ServiceProvider ?? options.EndpointBuilder.ApplicationServices;

        var factoryContext = new EndpointFilterFactoryContext
        {
            MethodInfo = requestDelegate.Method,
            ApplicationServices = options.EndpointBuilder.ApplicationServices
        };

        EndpointFilterDelegate<TRequestContext> filteredInvocation = async (EndpointFilterInvocationContext<TRequestContext> context) =>
        {
            if (context.RequestContext.Features.Get<IFailureFeature>() is not IFailureFeature responseFeature || !responseFeature.IsFailure)
            {
                await requestDelegate(context.RequestContext);
            }

            return null;
        };

        var initialFilteredInvocation = filteredInvocation;
        for (var i = options.EndpointBuilder.FilterFactories.Count - 1; i >= 0; i--)
        {
            var currentFilterFactory = options.EndpointBuilder.FilterFactories[i];
            filteredInvocation = currentFilterFactory(factoryContext, filteredInvocation);
        }

        // The filter factories have run without modifying per-request behavior, we can skip running the pipeline.
        if (ReferenceEquals(initialFilteredInvocation, filteredInvocation))
        {
            return requestDelegate;
        }

        return async (TRequestContext requestContext) =>
        {
            var obj = await filteredInvocation(new DefaultEndpointFilterInvocationContext<TRequestContext>(requestContext, [requestContext]));
            if (obj is not null)
            {
                await ExecuteHandlerHelper.ExecuteReturnAsync(obj, requestContext);
            }
        };
    }
}
