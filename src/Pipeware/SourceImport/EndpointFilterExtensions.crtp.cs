// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Builder/EndpointFilterExtensions.cs
// Source alias: crtp
// Source Sha256: 7ca0a7d98cb8ee8ba54b274438739aa0ff6cb1d5

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Pipeware.Builder;
using Pipeware.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Pipeware;

/// <summary>
/// Extension methods for adding <see cref="IEndpointFilter{TRequestContext}"/> to a route handler.
/// </summary>
public static partial class EndpointFilterExtensions
{
    /// <summary>
    /// Registers a filter onto the route handler.
    /// </summary>
    /// <param name="builder">The <see cref="RouteHandlerBuilder{TRequestContext}"/>.</param>
    /// <param name="filter">The <see cref="IEndpointFilter{TRequestContext}"/> to register.</param>
    /// <returns>A <see cref="RouteHandlerBuilder{TRequestContext}"/> that can be used to further customize the route handler.</returns>
    public static TBuilder AddEndpointFilter<TBuilder, TRequestContext>(this IEndpointConventionBuilder<TRequestContext, TBuilder> builder, IEndpointFilter<TRequestContext> filter) where TBuilder : IEndpointConventionBuilder<TRequestContext,TBuilder> where TRequestContext : class, IRequestContext =>
        builder.AddEndpointFilterFactory((routeHandlerContext, next) => (context) => filter.InvokeAsync(context, next));

    /// <summary>
    /// Registers a filter given a delegate onto the route handler.
    /// </summary>
    /// <param name="builder">The <see cref="RouteHandlerBuilder{TRequestContext}"/>.</param>
    /// <param name="routeHandlerFilter">A method representing the core logic of the filter.</param>
    /// <returns>A <see cref="RouteHandlerBuilder{TRequestContext}"/> that can be used to further customize the route handler.</returns>
    public static TBuilder AddEndpointFilter<TBuilder, TRequestContext>(this IEndpointConventionBuilder<TRequestContext, TBuilder> builder, Func<EndpointFilterInvocationContext<TRequestContext>, EndpointFilterDelegate<TRequestContext>, ValueTask<object?>> routeHandlerFilter)
        where TBuilder : IEndpointConventionBuilder<TRequestContext,TBuilder> where TRequestContext : class, IRequestContext
    {
        return builder.AddEndpointFilterFactory((routeHandlerContext, next) => (context) => routeHandlerFilter(context, next));
    }

    /// <summary>
    /// Register a filter given a delegate representing the filter factory.
    /// </summary>
    /// <param name="builder">The <see cref="RouteHandlerBuilder{TRequestContext}"/>.</param>
    /// <param name="filterFactory">A method representing the logic for constructing the filter.</param>
    /// <returns>A <see cref="RouteHandlerBuilder{TRequestContext}"/> that can be used to further customize the route handler.</returns>
    public static TBuilder AddEndpointFilterFactory<TBuilder, TRequestContext>(this IEndpointConventionBuilder<TRequestContext, TBuilder> builder, Func<EndpointFilterFactoryContext, EndpointFilterDelegate<TRequestContext>, EndpointFilterDelegate<TRequestContext>> filterFactory)
        where TBuilder : IEndpointConventionBuilder<TRequestContext,TBuilder> where TRequestContext : class, IRequestContext
    {
        builder.Add(endpointBuilder =>
        {
            endpointBuilder.FilterFactories.Add(filterFactory);
        });

        return (TBuilder)builder;
    }
}