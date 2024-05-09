// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing.Abstractions/src/VirtualPathContext.cs
// Source Sha256: 5259f710426b3934d8f9b4d6fd0d5080494c40f1

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;

namespace Pipeware.Routing;

/// <summary>
/// A context for virtual path generation operations.
/// </summary>
public class VirtualPathContext
{
    /// <summary>
    /// Creates a new instance of <see cref="VirtualPathContext"/>.
    /// </summary>
    /// <param name="httpContext">The <see cref="Http.IRequestContext"/> associated with the current request.</param>
    /// <param name="ambientValues">The set of route values associated with the current request.</param>
    /// <param name="values">The set of new values provided for virtual path generation.</param>
    public VirtualPathContext(
        IRequestContext requestContext,
        RouteValueDictionary ambientValues,
        RouteValueDictionary values)
        : this(requestContext, ambientValues, values, null)
    {
    }

    /// <summary>
    /// Creates a new instance of <see cref="VirtualPathContext"/>.
    /// </summary>
    /// <param name="httpContext">The <see cref="Http.IRequestContext"/> associated with the current request.</param>
    /// <param name="ambientValues">The set of route values associated with the current request.</param>
    /// <param name="values">The set of new values provided for virtual path generation.</param>
    /// <param name="routeName">The name of the route to use for virtual path generation.</param>
    public VirtualPathContext(
        IRequestContext requestContext,
        RouteValueDictionary ambientValues,
        RouteValueDictionary values,
        string? routeName)
    {
        RequestContext = requestContext;
        AmbientValues = ambientValues;
        Values = values;
        RouteName = routeName;
    }

    /// <summary>
    /// Gets the set of route values associated with the current request.
    /// </summary>
    public RouteValueDictionary AmbientValues { get; }

    /// <summary>
    /// Gets the <see cref="Http.IRequestContext"/> associated with the current request.
    /// </summary>
    public IRequestContext RequestContext { get; }

    /// <summary>
    /// Gets the name of the route to use for virtual path generation.
    /// </summary>
    public string? RouteName { get; }

    /// <summary>
    /// Gets or sets the set of new values provided for virtual path generation.
    /// </summary>
    public RouteValueDictionary Values { get; set; }
}