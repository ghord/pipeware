// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing.Abstractions/src/RouteContext.cs
// Source Sha256: a8ea97f595c794d73b7180cb681139c1fe97d19d

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;
using System.Diagnostics.CodeAnalysis;

namespace Pipeware.Routing;

/// <summary>
/// A context object for <see cref="IRouter.RouteAsync(RouteContext)"/>.
/// </summary>
public class RouteContext
{
    private RouteData _routeData;

    /// <summary>
    /// Creates a new instance of <see cref="RouteContext"/> for the provided <paramref name="httpContext"/>.
    /// </summary>
    /// <param name="httpContext">The <see cref="Http.IRequestContext"/> associated with the current request.</param>
    public RouteContext(IRequestContext? requestContext)
    {
        RequestContext = requestContext ?? throw new ArgumentNullException(nameof(requestContext));

        RouteData = new RouteData();
    }

    /// <summary>
    /// Gets or sets the handler for the request. An <see cref="IRouter"/> should set <see cref="Handler"/>
    /// when it matches.
    /// </summary>
    public RequestDelegate<IRequestContext>? Handler { get; set; }

    /// <summary>
    /// Gets the <see cref="Http.IRequestContext"/> associated with the current request.
    /// </summary>
    public IRequestContext RequestContext { get; }

    /// <summary>
    /// Gets or sets the <see cref="Routing.RouteData"/> associated with the current context.
    /// </summary>
    public RouteData RouteData
    {
        get
        {
            return _routeData;
        }
        [MemberNotNull(nameof(_routeData))] set
        {
            ArgumentNullException.ThrowIfNull(value);

            _routeData = value;
        }
    }
}