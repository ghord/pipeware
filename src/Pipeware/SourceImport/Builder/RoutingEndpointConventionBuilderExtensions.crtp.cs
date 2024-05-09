// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Builder/RoutingEndpointConventionBuilderExtensions.cs
// Source alias: crtp
// Source Sha256: 8e49dd054e88d16784a434d6ab0dd5e68d636d8c

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware.Metadata;
using Pipeware.Routing;


namespace Pipeware.Builder;

/// <summary>
/// Extension methods for adding routing metadata to endpoint instances using <see cref="IEndpointConventionBuilder{TRequestContext,TBuilder}"/>.
/// </summary>
public static partial class RoutingEndpointConventionBuilderExtensions
{

    /// <summary>
    /// Sets the <see cref="EndpointBuilder.DisplayName"/> to the provided <paramref name="displayName"/> for all
    /// builders created by <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder{TRequestContext,TBuilder}"/>.</param>
    /// <param name="displayName">The display name.</param>
    /// <returns>The <see cref="IEndpointConventionBuilder{TRequestContext,TBuilder}"/>.</returns>
    public static TBuilder WithDisplayName<TBuilder, TRequestContext>(this IEndpointConventionBuilder<TRequestContext, TBuilder> builder, string displayName) where TBuilder : IEndpointConventionBuilder<TRequestContext,TBuilder> where TRequestContext : class, IRequestContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Add(b =>
        {
            b.DisplayName = displayName;
        });

        return (TBuilder)builder;
    }

    /// <summary>
    /// Sets the <see cref="EndpointBuilder.DisplayName"/> using the provided <paramref name="func"/> for all
    /// builders created by <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder{TRequestContext,TBuilder}"/>.</param>
    /// <param name="func">A delegate that produces the display name for each <see cref="EndpointBuilder{TRequestContext}"/>.</param>
    /// <returns>The <see cref="IEndpointConventionBuilder{TRequestContext,TBuilder}"/>.</returns>
    public static TBuilder WithDisplayName<TBuilder, TRequestContext>(this IEndpointConventionBuilder<TRequestContext, TBuilder> builder, Func<EndpointBuilder<TRequestContext>, string> func) where TBuilder : IEndpointConventionBuilder<TRequestContext,TBuilder> where TRequestContext : class, IRequestContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(func);

        builder.Add(b =>
        {
            b.DisplayName = func(b);
        });

        return (TBuilder)builder;
    }

    /// <summary>
    /// Adds the provided metadata <paramref name="items"/> to <see cref="EndpointBuilder.Metadata"/> for all builders
    /// produced by <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder{TRequestContext,TBuilder}"/>.</param>
    /// <param name="items">A collection of metadata items.</param>
    /// <returns>The <see cref="IEndpointConventionBuilder{TRequestContext,TBuilder}"/>.</returns>
    public static TBuilder WithMetadata<TBuilder, TRequestContext>(this IEndpointConventionBuilder<TRequestContext, TBuilder> builder, params object[] items) where TBuilder : IEndpointConventionBuilder<TRequestContext,TBuilder> where TRequestContext : class, IRequestContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(items);

        builder.Add(b =>
        {
            foreach (var item in items)
            {
                b.Metadata.Add(item);
            }
        });

        return (TBuilder)builder;
    }

    /// <summary>
    /// Adds the <see cref="IEndpointNameMetadata"/> to the Metadata collection for all endpoints produced
    /// on the target <see cref="IEndpointConventionBuilder{TRequestContext,TBuilder}"/> given the <paramref name="endpointName" />.
    /// The <see cref="IEndpointNameMetadata" /> on the endpoint is used for link generation and
    /// is treated as the operation ID in the given endpoint's OpenAPI specification.
    /// </summary>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder{TRequestContext,TBuilder}"/>.</param>
    /// <param name="endpointName">The endpoint name.</param>
    /// <returns>The <see cref="IEndpointConventionBuilder{TRequestContext,TBuilder}"/>.</returns>
    public static TBuilder WithName<TBuilder, TRequestContext>(this IEndpointConventionBuilder<TRequestContext, TBuilder> builder, string endpointName) where TBuilder : IEndpointConventionBuilder<TRequestContext,TBuilder> where TRequestContext : class, IRequestContext
    {
        builder.WithMetadata(new EndpointNameMetadata(endpointName), new RouteNameMetadata(endpointName));
        return (TBuilder)builder;
    }

    /// <summary>
    /// Sets the <see cref="EndpointGroupNameAttribute"/> for all endpoints produced
    /// on the target <see cref="IEndpointConventionBuilder{TRequestContext,TBuilder}"/> given the <paramref name="endpointGroupName" />.
    /// The <see cref="IEndpointGroupNameMetadata" /> on the endpoint is used to set the endpoint's
    /// GroupName in the OpenAPI specification.
    /// </summary>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder{TRequestContext,TBuilder}"/>.</param>
    /// <param name="endpointGroupName">The endpoint group name.</param>
    /// <returns>The <see cref="IEndpointConventionBuilder{TRequestContext,TBuilder}"/>.</returns>
    public static TBuilder WithGroupName<TBuilder, TRequestContext>(this IEndpointConventionBuilder<TRequestContext, TBuilder> builder, string endpointGroupName) where TBuilder : IEndpointConventionBuilder<TRequestContext,TBuilder> where TRequestContext : class, IRequestContext
    {
        builder.WithMetadata(new EndpointGroupNameAttribute(endpointGroupName));
        return (TBuilder)builder;
    }

    /// <summary>
    /// Sets the <see cref="RouteEndpointBuilder.Order"/> to the provided <paramref name="order"/> for all
    /// builders created by <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder{TRequestContext,TBuilder}"/>.</param>
    /// <param name="order">The order assigned to the endpoint.</param>
    /// <returns>The <see cref="IEndpointConventionBuilder{TRequestContext,TBuilder}"/>.</returns>
    public static TBuilder WithOrder<TBuilder, TRequestContext>(this IEndpointConventionBuilder<TRequestContext, TBuilder> builder, int order) where TBuilder : IEndpointConventionBuilder<TRequestContext,TBuilder> where TRequestContext : class, IRequestContext
    {
        builder.Add(builder =>
        {
            if (builder is RouteEndpointBuilder<TRequestContext> routeEndpointBuilder)
            {
                routeEndpointBuilder.Order = order;
            }
            else
            {
                throw new InvalidOperationException("This endpoint does not support Order.");
            }
        });
        return (TBuilder)builder;
    }
}