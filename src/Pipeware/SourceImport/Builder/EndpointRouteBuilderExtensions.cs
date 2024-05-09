// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Builder/EndpointRouteBuilderExtensions.cs
// Source Sha256: a05c8006992adfb5a22aa471fc941dba8883186e

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Pipeware;
using Pipeware.Routing;
using Pipeware.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Pipeware.Builder;

/// <summary>
/// Provides extension methods for <see cref="IEndpointRouteBuilder{TRequestContext}"/> to add endpoints.
/// </summary>
public static partial class EndpointRouteBuilderExtensions
{
    private const string MapEndpointUnreferencedCodeWarning = "This API may perform reflection on the supplied delegate and its parameters. These types may be trimmed if not directly referenced.";
    private const string MapEndpointDynamicCodeWarning = "This API may perform reflection on the supplied delegate and its parameters. These types may require generated code and aren't compatible with native AOT applications.";

    /// <summary>
    /// Creates a <see cref="RouteGroupBuilder{TRequestContext}"/> for defining endpoints all prefixed with the specified <paramref name="prefix"/>.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder{TRequestContext}"/> to add the group to.</param>
    /// <param name="prefix">The pattern that prefixes all routes in this group.</param>
    /// <returns>
    /// A <see cref="RouteGroupBuilder{TRequestContext}"/> that is both an <see cref="IEndpointRouteBuilder{TRequestContext}"/> and an <see cref="IEndpointConventionBuilder{TRequestContext}"/>.
    /// The same builder can be used to add endpoints with the given <paramref name="prefix"/>, and to customize those endpoints using conventions.
    /// </returns>
    public static RouteGroupBuilder<TRequestContext> MapGroup<TRequestContext>(this IEndpointRouteBuilder<TRequestContext> endpoints, [StringSyntax("Route")] string prefix) where TRequestContext : class, IRequestContext =>
        endpoints.MapGroup<TRequestContext>(RoutePatternFactory.Parse(prefix ?? throw new ArgumentNullException(nameof(prefix))));

    /// <summary>
    /// Creates a <see cref="RouteGroupBuilder{TRequestContext}"/> for defining endpoints all prefixed with the specified <paramref name="prefix"/>.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder{TRequestContext}"/> to add the group to.</param>
    /// <param name="prefix">The pattern that prefixes all routes in this group.</param>
    /// <returns>
    /// A <see cref="RouteGroupBuilder{TRequestContext}"/> that is both an <see cref="IEndpointRouteBuilder{TRequestContext}"/> and an <see cref="IEndpointConventionBuilder{TRequestContext}"/>.
    /// The same builder can be used to add endpoints with the given <paramref name="prefix"/>, and to customize those endpoints using conventions.
    /// </returns>
    public static RouteGroupBuilder<TRequestContext> MapGroup<TRequestContext>(this IEndpointRouteBuilder<TRequestContext> endpoints, RoutePattern prefix) where TRequestContext : class, IRequestContext
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(prefix);

        return new(endpoints, prefix);
    }

    /// <summary>
    /// Adds a <see cref="RouteEndpoint{TRequestContext}"/> to the <see cref="IEndpointRouteBuilder{TRequestContext}"/> that matches HTTP requests
    /// for the specified pattern.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder{TRequestContext}"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="requestDelegate">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="IEndpointConventionBuilder{TRequestContext}"/> that can be used to further customize the endpoint.</returns>
    public static IEndpointConventionBuilder<TRequestContext> Map<TRequestContext>(
        this IEndpointRouteBuilder<TRequestContext> endpoints,
        [StringSyntax("Route")] string pattern,
        RequestDelegate<TRequestContext> requestDelegate) where TRequestContext : class, IRequestContext
    {
        return Map<TRequestContext>(endpoints, RoutePatternFactory.Parse(pattern), requestDelegate);
    }

    /// <summary>
    /// Adds a <see cref="RouteEndpoint{TRequestContext}"/> to the <see cref="IEndpointRouteBuilder{TRequestContext}"/> that matches HTTP requests
    /// for the specified pattern.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder{TRequestContext}"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="handler">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="RouteHandlerBuilder{TRequestContext}"/> that can be used to further customize the endpoint.</returns>
    [RequiresUnreferencedCode(MapEndpointUnreferencedCodeWarning)]
    [RequiresDynamicCode(MapEndpointDynamicCodeWarning)]
    public static RouteHandlerBuilder<TRequestContext> Map<TRequestContext>(
        this IEndpointRouteBuilder<TRequestContext> endpoints,
        [StringSyntax("Route")] string pattern,
        Delegate handler) where TRequestContext : class, IRequestContext
    {
        return Map<TRequestContext>(endpoints, RoutePatternFactory.Parse(pattern), handler);
    }

    /// <summary>
    /// Adds a specialized <see cref="RouteEndpoint{TRequestContext}"/> to the <see cref="IEndpointRouteBuilder{TRequestContext}"/> that will match
    /// requests for non-file-names with the lowest possible priority.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder{TRequestContext}"/> to add the route to.</param>
    /// <param name="handler">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="RouteHandlerBuilder{TRequestContext}"/> that can be used to further customize the endpoint.</returns>
    /// <remarks>
    /// <para>
    /// <see cref="MapFallback(IEndpointRouteBuilder, Delegate)"/> is intended to handle cases where URL path of
    /// the request does not contain a file name, and no other endpoint has matched. This is convenient for routing
    /// requests for dynamic content to a SPA framework, while also allowing requests for non-existent files to
    /// result in an HTTP 404.
    /// </para>
    /// <para>
    /// <see cref="MapFallback(IEndpointRouteBuilder, Delegate)"/> registers an endpoint using the pattern
    /// <c>{*path:nonfile}</c>. The order of the registered endpoint will be <c>int.MaxValue</c>.
    /// </para>
    /// </remarks>
    [RequiresUnreferencedCode(MapEndpointUnreferencedCodeWarning)]
    [RequiresDynamicCode(MapEndpointDynamicCodeWarning)]
    public static RouteHandlerBuilder<TRequestContext> MapFallback<TRequestContext>(this IEndpointRouteBuilder<TRequestContext> endpoints, Delegate handler) where TRequestContext : class, IRequestContext
    {
        return endpoints.MapFallback<TRequestContext>("{*path:nonfile}", handler);
    }

    /// <summary>
    /// Adds a specialized <see cref="RouteEndpoint{TRequestContext}"/> to the <see cref="IEndpointRouteBuilder{TRequestContext}"/> that will match
    /// the provided pattern with the lowest possible priority.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder{TRequestContext}"/> to add the route to.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <param name="handler">The delegate executed when the endpoint is matched.</param>
    /// <returns>A <see cref="RouteHandlerBuilder{TRequestContext}"/> that can be used to further customize the endpoint.</returns>
    /// <remarks>
    /// <para>
    /// <see cref="MapFallback(IEndpointRouteBuilder, string, Delegate)"/> is intended to handle cases where no
    /// other endpoint has matched. This is convenient for routing requests to a SPA framework.
    /// </para>
    /// <para>
    /// The order of the registered endpoint will be <c>int.MaxValue</c>.
    /// </para>
    /// <para>
    /// This overload will use the provided <paramref name="pattern"/> verbatim. Use the <c>:nonfile</c> route constraint
    /// to exclude requests for static files.
    /// </para>
    /// </remarks>
    [RequiresUnreferencedCode(MapEndpointUnreferencedCodeWarning)]
    [RequiresDynamicCode(MapEndpointDynamicCodeWarning)]
    public static RouteHandlerBuilder<TRequestContext> MapFallback<TRequestContext>(
        this IEndpointRouteBuilder<TRequestContext> endpoints,
        [StringSyntax("Route")] string pattern,
        Delegate handler) where TRequestContext : class, IRequestContext
    {
        return endpoints.Map<TRequestContext>(RoutePatternFactory.Parse(pattern), handler, isFallback: true);
    }

    internal static RouteEndpointDataSource<TRequestContext> GetOrAddRouteEndpointDataSource<TRequestContext>(this IEndpointRouteBuilder<TRequestContext> endpoints) where TRequestContext : class, IRequestContext
    {
        RouteEndpointDataSource<TRequestContext>? routeEndpointDataSource = null;

        foreach (var dataSource in endpoints.DataSources)
        {
            if (dataSource is RouteEndpointDataSource<TRequestContext> foundDataSource)
            {
                routeEndpointDataSource = foundDataSource;
                break;
            }
        }

        if (routeEndpointDataSource is null)
        {
            // ServiceProvider isn't nullable, but it is being called by methods that historically did not access this property, so we null check anyway.
            var routeHandlerOptions = endpoints.ServiceProvider?.GetService<IOptions<RouteHandlerOptions>>();
            var throwOnBadRequest = routeHandlerOptions?.Value.ThrowOnBadRequest ?? false;

            routeEndpointDataSource = new RouteEndpointDataSource<TRequestContext>(endpoints.ServiceProvider ?? EmptyServiceProvider.Instance, throwOnBadRequest);
            endpoints.DataSources.Add(routeEndpointDataSource);
        }

        return routeEndpointDataSource;
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new EmptyServiceProvider();
        public object? GetService(Type serviceType) => null;
    }
}