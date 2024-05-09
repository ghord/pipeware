// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Constraints/RequiredRouteConstraint.cs
// Source Sha256: 9e379dadec0b72194761d062c0e4aa9ad915f5fe

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Pipeware;

namespace Pipeware.Routing.Constraints;

/// <summary>
/// Constraints a route parameter that must have a value.
/// </summary>
/// <remarks>
/// This constraint is primarily used to enforce that a non-parameter value is present during
/// URL generation.
/// </remarks>
public class RequiredRouteConstraint : IRouteConstraint
{
    /// <inheritdoc />
    public bool Match(
        IRequestContext? requestContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
    {
        ArgumentNullException.ThrowIfNull(routeKey);
        ArgumentNullException.ThrowIfNull(values);

        if (values.TryGetValue(routeKey, out var value) && value != null)
        {
            // In routing the empty string is equivalent to null, which is equivalent to an unset value.
            var valueString = Convert.ToString(value, CultureInfo.InvariantCulture);
            return !string.IsNullOrEmpty(valueString);
        }

        return false;
    }
}