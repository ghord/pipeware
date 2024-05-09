// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Constraints/GuidRouteConstraint.cs
// Source Sha256: 358f3758b4fc750a76b901f36200abd23877285e

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
#if !COMPONENTS
using Pipeware;
using Pipeware.Routing.Matching;
#else
using Microsoft.AspNetCore.Components.Routing;
#endif

namespace Pipeware.Routing.Constraints;

#if !COMPONENTS
/// <summary>
/// Constrains a route parameter to represent only <see cref="Guid"/> values.
/// Matches values specified in any of the five formats "N", "D", "B", "P", or "X",
/// supported by Guid.ToString(string) and Guid.ToString(String, IFormatProvider) methods.
/// </summary>
public class GuidRouteConstraint : IRouteConstraint, IParameterLiteralNodeMatchingPolicy, ICachableParameterPolicy
#else
internal class GuidRouteConstraint : IRouteConstraint
#endif
{
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

        if (values.TryGetValue(routeKey, out var value) && value != null)
        {
            if (value is Guid)
            {
                return true;
            }

            var valueString = Convert.ToString(value, CultureInfo.InvariantCulture);
            return CheckConstraintCore(valueString);
        }

        return false;
    }

    private static bool CheckConstraintCore(string? valueString)
    {
        return Guid.TryParse(valueString, out _);
    }

#if !COMPONENTS
    bool IParameterLiteralNodeMatchingPolicy.MatchesLiteral(string parameterName, string literal)
    {
        return CheckConstraintCore(literal);
    }
#endif
}