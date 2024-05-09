// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/DependencyInjection/RoutingServiceCollectionExtensions.cs
// Source Sha256: dcbcc2e123bc12a75616a0dab3e04c888489b9fa

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using Pipeware.Routing;
using Pipeware.Routing.Internal;
using Pipeware.Routing.Matching;
using Pipeware.Routing.Patterns;
using Pipeware.Routing.Template;
using Pipeware.Routing.Tree;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Pipeware.Routing;

/// <summary>
/// Contains extension methods to <see cref="IServiceCollection"/>.
/// </summary>
public static class RoutingServiceCollectionExtensions
{
    /// <summary>
    /// Adds services required for routing requests.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddRouting<TRequestContext>(this IServiceCollection services) where TRequestContext : class, IRequestContext
    {
        services.AddRoutingCore<TRequestContext>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<RouteOptions<TRequestContext>>, RegexInlineRouteConstraintSetup<TRequestContext>>());
        return services;
    }

    /// <summary>
    /// Adds services required for routing requests. This is similar to
    /// <see cref="AddRouting(IServiceCollection)" /> except that it
    /// excludes certain options that can be opted in separately, if needed.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddRoutingCore<TRequestContext>(this IServiceCollection services) where TRequestContext : class, IRequestContext
    {
        ArgumentNullException.ThrowIfNull(services);

        // Required for IMeterFactory dependency.
        services.AddMetrics();

        services.TryAddTransient<IInlineConstraintResolver<TRequestContext>, DefaultInlineConstraintResolver<TRequestContext>>();
        services.TryAddTransient<ObjectPoolProvider, DefaultObjectPoolProvider>();
        services.TryAddSingleton<ObjectPool<UriBuildingContext>>(s =>
        {
            var provider = s.GetRequiredService<ObjectPoolProvider>();
            return provider.Create<UriBuildingContext>(new UriBuilderContextPooledObjectPolicy());
        });

        // The TreeRouteBuilder is a builder for creating routes, it should stay transient because it's
        // stateful.
        services.TryAdd(ServiceDescriptor.Transient<TreeRouteBuilder>(s =>
        {
            var loggerFactory = s.GetRequiredService<ILoggerFactory>();
            var objectPool = s.GetRequiredService<ObjectPool<UriBuildingContext>>();
            var constraintResolver = s.GetRequiredService<IInlineConstraintResolver<TRequestContext>>();
            return new TreeRouteBuilder(loggerFactory, objectPool, constraintResolver);
        }));

        services.TryAddSingleton(typeof(RoutingMarkerService<TRequestContext>));

        // Setup global collection of endpoint data sources
        var dataSources = new ObservableCollection<EndpointDataSource<TRequestContext>>();
        services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<RouteOptions<TRequestContext>>, ConfigureRouteOptions<TRequestContext>>(
            serviceProvider => new ConfigureRouteOptions<TRequestContext>(dataSources)));

        // Allow global access to the list of endpoints.
        services.TryAddSingleton<EndpointDataSource<TRequestContext>>(s =>
        {
            // Call internal ctor and pass global collection
            return new CompositeEndpointDataSource<TRequestContext>(dataSources);
        });

        //
        // Default matcher implementation
        //
        services.TryAddSingleton<ParameterPolicyFactory<TRequestContext>, DefaultParameterPolicyFactory<TRequestContext>>();
        services.TryAddSingleton<MatcherFactory<TRequestContext>, DfaMatcherFactory<TRequestContext>>();
        services.TryAddTransient<DfaMatcherBuilder<TRequestContext>>();
        services.TryAddSingleton<DfaGraphWriter<TRequestContext>>();
        services.TryAddTransient<DataSourceDependentMatcher<TRequestContext>.Lifetime>();
        services.TryAddSingleton<EndpointMetadataComparer<TRequestContext>>(services =>
        {
            // This has no public constructor.
            return new EndpointMetadataComparer<TRequestContext>(services);
        });

        // Link generation related services
        services.TryAddSingleton<LinkGenerator<TRequestContext>, DefaultLinkGenerator<TRequestContext>>();
        services.TryAddSingleton<IEndpointAddressScheme<string, TRequestContext>, EndpointNameAddressScheme<TRequestContext>>();
        services.TryAddSingleton<IEndpointAddressScheme<RouteValuesAddress, TRequestContext>, RouteValuesAddressScheme<TRequestContext>>();
        services.TryAddSingleton<LinkParser, DefaultLinkParser<TRequestContext>>();

        //
        // Endpoint Selection
        //
        services.TryAddSingleton<EndpointSelector<TRequestContext>, DefaultEndpointSelector<TRequestContext>>();

        //
        // Misc infrastructure
        //
        services.TryAddSingleton<TemplateBinderFactory<TRequestContext>, DefaultTemplateBinderFactory<TRequestContext>>();
        services.TryAddSingleton<RoutePatternTransformer<TRequestContext>, DefaultRoutePatternTransformer<TRequestContext>>();
        services.TryAddSingleton<RoutingMetrics>();

        // Set RouteHandlerOptions.ThrowOnBadRequest in development
        services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<RouteHandlerOptions>, ConfigureRouteHandlerOptions>());

        return services;
    }

    /// <summary>
    /// Adds services required for routing requests.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configureOptions">The routing options to configure the middleware with.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddRouting<TRequestContext>(
        this IServiceCollection services,
        Action<RouteOptions<TRequestContext>> configureOptions) where TRequestContext : class, IRequestContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);
        services.AddRouting<TRequestContext>();

        return services;
    }
}