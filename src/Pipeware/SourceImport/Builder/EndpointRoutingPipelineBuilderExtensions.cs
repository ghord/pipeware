// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Builder/EndpointRoutingApplicationBuilderExtensions.cs
// Source Sha256: f8fbd3864f89e457b60462cbcdbc7650884c3f96

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;
using Pipeware.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Pipeware.Builder;

/// <summary>
/// Contains extensions for configuring routing on an <see cref="IPipelineBuilder{TRequestContext}"/>.
/// </summary>
public static class EndpointRoutingPipelineBuilderExtensions
{
    private const string EndpointRouteBuilder = "__EndpointRouteBuilder";
    private const string GlobalEndpointRouteBuilderKey = "__GlobalEndpointRouteBuilder";
    private const string UseRoutingKey = "__UseRouting";

    /// <summary>
    /// Adds a <see cref="EndpointRoutingMiddleware{TRequestContext}"/> middleware to the specified <see cref="IPipelineBuilder{TRequestContext}"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IPipelineBuilder{TRequestContext}"/> to add the middleware to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    /// <remarks>
    /// <para>
    /// A call to <see cref="UseRouting(IApplicationBuilder)"/> must be followed by a call to
    /// <see cref="UseEndpoints(IApplicationBuilder, Action{IEndpointRouteBuilder<TRequestContext>})"/> for the same <see cref="IPipelineBuilder{TRequestContext}"/>
    /// instance.
    /// </para>
    /// <para>
    /// The <see cref="EndpointRoutingMiddleware{TRequestContext}"/> defines a point in the middleware pipeline where routing decisions are
    /// made, and an <see cref="Endpoint{TRequestContext}"/> is associated with the <see cref="TRequestContext"/>. The <see cref="EndpointMiddleware{TRequestContext}"/>
    /// defines a point in the middleware pipeline where the current <see cref="Endpoint{TRequestContext}"/> is executed. Middleware between
    /// the <see cref="EndpointRoutingMiddleware{TRequestContext}"/> and <see cref="EndpointMiddleware{TRequestContext}"/> may observe or change the
    /// <see cref="Endpoint{TRequestContext}"/> associated with the <see cref="TRequestContext"/>.
    /// </para>
    /// </remarks>
    public static IPipelineBuilder<TRequestContext> UseRouting<TRequestContext>(this IPipelineBuilder<TRequestContext> builder) where TRequestContext : class, IRequestContext
    {
        ArgumentNullException.ThrowIfNull(builder);

        VerifyRoutingServicesAreRegistered<TRequestContext>(builder);

        IEndpointRouteBuilder<TRequestContext> endpointRouteBuilder;
        if (builder.Properties.TryGetValue(GlobalEndpointRouteBuilderKey, out var obj))
        {
            endpointRouteBuilder = (IEndpointRouteBuilder<TRequestContext>)obj!;
            // Let interested parties know if UseRouting() was called while a global route builder was set
            builder.Properties[EndpointRouteBuilder] = endpointRouteBuilder;
        }
        else
        {
            endpointRouteBuilder = new DefaultEndpointRouteBuilder<TRequestContext>(builder);
            builder.Properties[EndpointRouteBuilder] = endpointRouteBuilder;
        }

        // Add UseRouting function to properties so that middleware that can't reference UseRouting directly can call UseRouting via this property
        // This is part of the global endpoint route builder concept
        builder.Properties.TryAdd(UseRoutingKey, (object)UseRouting<TRequestContext>);

        return builder.UseMiddleware(typeof(EndpointRoutingMiddleware<TRequestContext>), endpointRouteBuilder);
    }

    /// <summary>
    /// Adds a <see cref="EndpointMiddleware{TRequestContext}"/> middleware to the specified <see cref="IPipelineBuilder{TRequestContext}"/>
    /// with the <see cref="EndpointDataSource{TRequestContext}"/> instances built from configured <see cref="IEndpointRouteBuilder{TRequestContext}"/>.
    /// The <see cref="EndpointMiddleware{TRequestContext}"/> will execute the <see cref="Endpoint{TRequestContext}"/> associated with the current
    /// request.
    /// </summary>
    /// <param name="builder">The <see cref="IPipelineBuilder{TRequestContext}"/> to add the middleware to.</param>
    /// <param name="configure">An <see cref="Action{IEndpointRouteBuilder<TRequestContext>}"/> to configure the provided <see cref="IEndpointRouteBuilder{TRequestContext}"/>.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    /// <remarks>
    /// <para>
    /// A call to <see cref="UseEndpoints(IApplicationBuilder, Action{IEndpointRouteBuilder<TRequestContext>})"/> must be preceded by a call to
    /// <see cref="UseRouting(IApplicationBuilder)"/> for the same <see cref="IPipelineBuilder{TRequestContext}"/>
    /// instance.
    /// </para>
    /// <para>
    /// The <see cref="EndpointRoutingMiddleware{TRequestContext}"/> defines a point in the middleware pipeline where routing decisions are
    /// made, and an <see cref="Endpoint{TRequestContext}"/> is associated with the <see cref="TRequestContext"/>. The <see cref="EndpointMiddleware{TRequestContext}"/>
    /// defines a point in the middleware pipeline where the current <see cref="Endpoint{TRequestContext}"/> is executed. Middleware between
    /// the <see cref="EndpointRoutingMiddleware{TRequestContext}"/> and <see cref="EndpointMiddleware{TRequestContext}"/> may observe or change the
    /// <see cref="Endpoint{TRequestContext}"/> associated with the <see cref="TRequestContext"/>.
    /// </para>
    /// </remarks>
    public static IPipelineBuilder<TRequestContext> UseEndpoints<TRequestContext>(this IPipelineBuilder<TRequestContext> builder, Action<IEndpointRouteBuilder<TRequestContext>> configure) where TRequestContext : class, IRequestContext
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        VerifyRoutingServicesAreRegistered<TRequestContext>(builder);

        VerifyEndpointRoutingMiddlewareIsRegistered<TRequestContext>(builder, out var endpointRouteBuilder);

        configure(endpointRouteBuilder);

        // Yes, this mutates an IOptions. We're registering data sources in a global collection which
        // can be used for discovery of endpoints or URL generation.
        //
        // Each middleware gets its own collection of data sources, and all of those data sources also
        // get added to a global collection.
        var routeOptions = builder.ApplicationServices.GetRequiredService<IOptions<RouteOptions<TRequestContext>>>();
        foreach (var dataSource in endpointRouteBuilder.DataSources)
        {
            if (!routeOptions.Value.EndpointDataSources.Contains(dataSource))
            {
                routeOptions.Value.EndpointDataSources.Add(dataSource);
            }
        }

        return builder.UseMiddleware(typeof(EndpointMiddleware<TRequestContext>));
    }

    private static void VerifyRoutingServicesAreRegistered<TRequestContext>(IPipelineBuilder<TRequestContext> app) where TRequestContext : class, IRequestContext
    {
        // Verify if AddRouting was done before calling UseEndpointRouting/UseEndpoint
        // We use the RoutingMarkerService to make sure if all the services were added.
        if (app.ApplicationServices.GetService(typeof(RoutingMarkerService<TRequestContext>)) == null)
        {
            throw new InvalidOperationException(string.Format("Unable to find the required services. Please add all the required services by calling '{0}.{1}' inside the call to '{2}' in the application startup code.", nameof(IServiceCollection), nameof(RoutingServiceCollectionExtensions.AddRouting), "ConfigureServices(...)"));
        }
    }

    private static void VerifyEndpointRoutingMiddlewareIsRegistered<TRequestContext>(IPipelineBuilder<TRequestContext> app, out IEndpointRouteBuilder<TRequestContext> endpointRouteBuilder) where TRequestContext : class, IRequestContext
    {
        if (!app.Properties.TryGetValue(EndpointRouteBuilder, out var obj))
        {
            var message =
                $"{nameof(EndpointRoutingMiddleware<TRequestContext>)} matches endpoints setup by {nameof(EndpointMiddleware<TRequestContext>)} and so must be added to the request " +
                $"execution pipeline before {nameof(EndpointMiddleware<TRequestContext>)}. " +
                $"Please add {nameof(EndpointRoutingMiddleware<TRequestContext>)} by calling '{nameof(IPipelineBuilder<TRequestContext>)}.{nameof(UseRouting)}' inside the call " +
                $"to 'Configure(...)' in the application startup code.";
            throw new InvalidOperationException(message);
        }

        endpointRouteBuilder = (IEndpointRouteBuilder<TRequestContext>)obj!;

        // This check handles the case where Map or something else that forks the pipeline is called between the two
        // routing middleware.
        if (endpointRouteBuilder is DefaultEndpointRouteBuilder<TRequestContext> defaultRouteBuilder && !object.ReferenceEquals(app, defaultRouteBuilder.ApplicationBuilder))
        {
            var message =
                $"The {nameof(EndpointRoutingMiddleware<TRequestContext>)} and {nameof(EndpointMiddleware<TRequestContext>)} must be added to the same {nameof(IPipelineBuilder<TRequestContext>)} instance. " +
                $"To use Endpoint Routing with 'Map(...)', make sure to call '{nameof(IPipelineBuilder<TRequestContext>)}.{nameof(UseRouting)}' before " +
                $"'{nameof(IPipelineBuilder<TRequestContext>)}.{nameof(UseEndpoints)}' for each branch of the middleware pipeline.";
            throw new InvalidOperationException(message);
        }
    }
}