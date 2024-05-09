// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Constraints/LengthRouteConstraint.cs
// Source Sha256: dd88703b38c2bb06092c803792420ff3336d4da5

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
/// Constrains a route parameter to be a string of a given length or within a given range of lengths.
/// </summary>
public class LengthRouteConstraint : IRouteConstraint, IParameterLiteralNodeMatchingPolicy, ICachableParameterPolicy
#else
internal class LengthRouteConstraint : IRouteConstraint
#endif
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LengthRouteConstraint" /> class that constrains
    /// a route parameter to be a string of a given length.
    /// </summary>
    /// <param name="length">The length of the route parameter.</param>
    public LengthRouteConstraint(int length)
    {
        if (length < 0)
        {
            var errorMessage = string.Format("Value must be greater than or equal to {0}.", 0);
            throw new ArgumentOutOfRangeException(nameof(length), length, errorMessage);
        }

        MinLength = MaxLength = length;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LengthRouteConstraint" /> class that constrains
    /// a route parameter to be a string of a given length.
    /// </summary>
    /// <param name="minLength">The minimum length allowed for the route parameter.</param>
    /// <param name="maxLength">The maximum length allowed for the route parameter.</param>
    public LengthRouteConstraint(int minLength, int maxLength)
    {
        if (minLength < 0)
        {
            var errorMessage = string.Format("Value must be greater than or equal to {0}.", 0);
            throw new ArgumentOutOfRangeException(nameof(minLength), minLength, errorMessage);
        }

        if (maxLength < 0)
        {
            var errorMessage = string.Format("Value must be greater than or equal to {0}.", 0);
            throw new ArgumentOutOfRangeException(nameof(maxLength), maxLength, errorMessage);
        }

        if (minLength > maxLength)
        {
            var errorMessage =
string.Format("The value for argument '{0}' should be less than or equal to the value for the argument '{1}'.", "minLength", "maxLength");
            throw new ArgumentOutOfRangeException(nameof(minLength), minLength, errorMessage);
        }

        MinLength = minLength;
        MaxLength = maxLength;
    }

    /// <summary>
    /// Gets the minimum length allowed for the route parameter.
    /// </summary>
    public int MinLength { get; }

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
        var length = valueString.Length;
        return length >= MinLength && length <= MaxLength;
    }

#if !COMPONENTS
    bool IParameterLiteralNodeMatchingPolicy.MatchesLiteral(string parameterName, string literal)
    {
        return CheckConstraintCore(literal);
    }
#endif
}