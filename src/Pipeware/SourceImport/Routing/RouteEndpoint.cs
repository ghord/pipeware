// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/RouteEndpoint.cs
// Source Sha256: a60d6b11995cc58d45604d993493c1094765bbd3

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;
using Pipeware.Routing.Patterns;

namespace Pipeware.Routing;

/// <summary>
/// Represents an <see cref="Endpoint{TRequestContext}"/> that can be used in URL matching or URL generation.
/// </summary>
public sealed class RouteEndpoint<TRequestContext> : Endpoint<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RouteEndpoint{TRequestContext}"/> class.
    /// </summary>
    /// <param name="requestDelegate">The delegate used to process requests for the endpoint.</param>
    /// <param name="routePattern">The <see cref="RoutePattern"/> to use in URL matching.</param>
    /// <param name="order">The order assigned to the endpoint.</param>
    /// <param name="metadata">
    /// The <see cref="EndpointMetadataCollection"/> or metadata associated with the endpoint.
    /// </param>
    /// <param name="displayName">The informational display name of the endpoint.</param>
    public RouteEndpoint(
        RequestDelegate<TRequestContext> requestDelegate,
        RoutePattern routePattern,
        int order,
        EndpointMetadataCollection? metadata,
        string? displayName)
        : base(requestDelegate, metadata, displayName)
    {
        ArgumentNullException.ThrowIfNull(requestDelegate);
        ArgumentNullException.ThrowIfNull(routePattern);

        RoutePattern = routePattern;
        Order = order;
    }

    /// <summary>
    /// Gets the order value of endpoint.
    /// </summary>
    /// <remarks>
    /// The order value provides absolute control over the priority
    /// of an endpoint. Endpoints with a lower numeric value of order have higher priority.
    /// </remarks>
    public int Order { get; }

    /// <summary>
    /// Gets the <see cref="RoutePattern"/> associated with the endpoint.
    /// </summary>
    public RoutePattern RoutePattern { get; }
}