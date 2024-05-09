// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/RouteEndpointBuilder.cs
// Source Sha256: 0f0cf78b642b12b8a33096cb07a8a267c46c0828

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Pipeware.Builder;

using Pipeware;
using Pipeware.Metadata;
using Pipeware.Routing.Patterns;
using Pipeware.Internal;

namespace Pipeware.Routing;

/// <summary>
/// Supports building a new <see cref="RouteEndpoint{TRequestContext}"/>.
/// </summary>
public sealed partial class RouteEndpointBuilder<TRequestContext> : EndpointBuilder<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Gets or sets the <see cref="RoutePattern"/> associated with this endpoint.
    /// </summary>
    public RoutePattern RoutePattern { get; set; }

    /// <summary>
    /// Gets or sets the order assigned to the endpoint.
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Constructs a new <see cref="RouteEndpointBuilder{TRequestContext}"/> instance.
    /// </summary>
    /// <param name="requestDelegate">The delegate used to process requests for the endpoint.</param>
    /// <param name="routePattern">The <see cref="RoutePattern"/> to use in URL matching.</param>
    /// <param name="order">The order assigned to the endpoint.</param>
    public RouteEndpointBuilder(
       RequestDelegate<TRequestContext>? requestDelegate,
       RoutePattern routePattern,
       int order)
    {
        ArgumentNullException.ThrowIfNull(routePattern);

        RequestDelegate = requestDelegate;
        RoutePattern = routePattern;
        Order = order;
    }

    /// <inheritdoc />
    public override Endpoint<TRequestContext> Build()
    {
        if (RequestDelegate is null)
        {
            throw new InvalidOperationException($"{nameof(RequestDelegate<TRequestContext>)} must be specified to construct a {nameof(RouteEndpoint<TRequestContext>)}.");
        }

        return new RouteEndpoint<TRequestContext>(
            RequestDelegate,
            RoutePattern,
            Order,
            CreateMetadataCollection(Metadata, RoutePattern),
            DisplayName);
    }

    [DebuggerDisplay("{ToString(),nq}")]
    private sealed class RouteDiagnosticsMetadata : IRouteDiagnosticsMetadata
    {
        public string Route { get; }

        public RouteDiagnosticsMetadata(string route)
        {
            Route = route;
        }

        public override string ToString()
        {
            return DebuggerHelpers.GetDebugText(nameof(Route), Route);
        }
    }
}