// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Constraints/OptionalRouteConstraint.cs
// Source Sha256: 6cc92f683d9763113fe9e2f0eb78bddd1a247322

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !COMPONENTS
using Pipeware;
#else
using Microsoft.AspNetCore.Components.Routing;
#endif

namespace Pipeware.Routing.Constraints;

#if !COMPONENTS
/// <summary>
/// Defines a constraint on an optional parameter. If the parameter is present, then it is constrained by InnerConstraint.
/// </summary>
public class OptionalRouteConstraint : IRouteConstraint
#else
internal class OptionalRouteConstraint : IRouteConstraint
#endif
{
    /// <summary>
    /// Creates a new <see cref="OptionalRouteConstraint"/> instance given the <paramref name="innerConstraint"/>.
    /// </summary>
    /// <param name="innerConstraint"></param>
    public OptionalRouteConstraint(IRouteConstraint innerConstraint)
    {
        ArgumentNullException.ThrowIfNull(innerConstraint);

        InnerConstraint = innerConstraint;
    }

    /// <summary>
    /// Gets the <see cref="IRouteConstraint"/> associated with the optional parameter.
    /// </summary>
    public IRouteConstraint InnerConstraint { get; }

    /// <inheritdoc />
    public bool Match(
#if !COMPONENTS
        IRequestContext? requestContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
#else
        string routeKey,
        RouteValueDictionary values)
#endif
    {
        ArgumentNullException.ThrowIfNull(routeKey);
        ArgumentNullException.ThrowIfNull(values);

        if (values.TryGetValue(routeKey, out _))
        {
            return InnerConstraint.Match(
#if !COMPONENTS
                requestContext,
                route,
#endif
                routeKey,
#if !COMPONENTS
                values,
                routeDirection);
#else
                values);
#endif
        }

        return true;
    }
}