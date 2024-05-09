// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Constraints/CompositeRouteConstraint.cs
// Source Sha256: b02cf1eea10792ede976d915b368ab5308ee29d8

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !COMPONENTS
using Pipeware;
using Pipeware.Routing.Matching;
#else
using Microsoft.AspNetCore.Components.Routing;
#endif
namespace Pipeware.Routing.Constraints;

#if !COMPONENTS
/// <summary>
/// Constrains a route by several child constraints.
/// </summary>
public class CompositeRouteConstraint : IRouteConstraint, IParameterLiteralNodeMatchingPolicy
#else
internal class CompositeRouteConstraint : IRouteConstraint
#endif
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeRouteConstraint" /> class.
    /// </summary>
    /// <param name="constraints">The child constraints that must match for this constraint to match.</param>
    public CompositeRouteConstraint(IEnumerable<IRouteConstraint> constraints)
    {
        ArgumentNullException.ThrowIfNull(constraints);

        Constraints = constraints;
    }

    /// <summary>
    /// Gets the child constraints that must match for this constraint to match.
    /// </summary>
    public IEnumerable<IRouteConstraint> Constraints { get; private set; }

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

        foreach (var constraint in Constraints)
        {
#if !COMPONENTS
            if (!constraint.Match(requestContext, route, routeKey, values, routeDirection))
#else
            if (!constraint.Match(routeKey, values))
#endif
            {
                return false;
            }
        }

        return true;
    }

#if !COMPONENTS
    bool IParameterLiteralNodeMatchingPolicy.MatchesLiteral(string parameterName, string literal)
    {
        foreach (var constraint in Constraints)
        {
            if (constraint is IParameterLiteralNodeMatchingPolicy literalConstraint && !literalConstraint.MatchesLiteral(parameterName, literal))
            {
                return false;
            }
        }

        return true;
    }
#endif
}