// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Constraints/MaxLengthRouteConstraint.cs
// Source Sha256: 66b6d3cf1d9a761a4afaeab98aa974a7ff49274a

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
/// Constrains a route parameter to be a string with a maximum length.
/// </summary>
public class MaxLengthRouteConstraint : IRouteConstraint, IParameterLiteralNodeMatchingPolicy, ICachableParameterPolicy
#else
internal class MaxLengthRouteConstraint : IRouteConstraint
#endif
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MaxLengthRouteConstraint" /> class.
    /// </summary>
    /// <param name="maxLength">The maximum length allowed for the route parameter.</param>
    public MaxLengthRouteConstraint(int maxLength)
    {
        if (maxLength < 0)
        {
            var errorMessage = string.Format("Value must be greater than or equal to {0}.", 0);
            throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, errorMessage);
        }

        MaxLength = maxLength;
    }

    /// <summary>
    /// Gets the maximum length allowed for the route parameter.
    /// </summary>
    public int MaxLength { get; }

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
            var valueString = Convert.ToString(value, CultureInfo.InvariantCulture)!;
            return CheckConstraintCore(valueString);
        }

        return false;
    }

    private bool CheckConstraintCore(string valueString)
    {
        return valueString.Length <= MaxLength;
    }

#if !COMPONENTS
    bool IParameterLiteralNodeMatchingPolicy.MatchesLiteral(string parameterName, string literal)
    {
        return CheckConstraintCore(literal);
    }
#endif
}