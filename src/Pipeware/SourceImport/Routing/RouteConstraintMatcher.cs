// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/RouteConstraintMatcher.cs
// Source Sha256: 4736edfc5a7c7aa7147cb6830ca5c9872f61d747

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !COMPONENTS
using Pipeware;
using Microsoft.Extensions.Logging;
#else
using Microsoft.AspNetCore.Components.Routing;
#endif

namespace Pipeware.Routing;

#if !COMPONENTS
/// <summary>
/// Use to evaluate if all route parameter values match their constraints.
/// </summary>
public static partial class RouteConstraintMatcher
#else
internal static partial class RouteConstraintMatcher
#endif
{
#if !COMPONENTS
    /// <summary>
    /// Determines if <paramref name="routeValues"/> match the provided <paramref name="constraints"/>.
    /// </summary>
    /// <param name="constraints">The constraints for the route.</param>
    /// <param name="routeValues">The route parameter values extracted from the matched route.</param>
    /// <param name="httpContext">The <see cref="IRequestContext"/> associated with the current request.</param>
    /// <param name="route">The router that this constraint belongs to.</param>
    /// <param name="routeDirection">
    /// Indicates whether the constraint check is performed
    /// when the incoming request is handled or when a URL is generated.
    /// </param>
    /// <param name="logger">The <see cref="ILogger{TCategoryName}"/>.</param>
    /// <returns><see langword="true"/> if the all route values match their constraints.</returns>
    public static bool Match(
        IDictionary<string, IRouteConstraint> constraints,
        RouteValueDictionary routeValues,
        IRequestContext requestContext,
        IRouter route,
        RouteDirection routeDirection,
        ILogger logger)
#else
    public static bool Match(
        IDictionary<string, IRouteConstraint> constraints,
        RouteValueDictionary routeValues)
#endif
    {
        ArgumentNullException.ThrowIfNull(routeValues);
#if !COMPONENTS
        ArgumentNullException.ThrowIfNull(requestContext);
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(logger);
#endif

        if (constraints == null || constraints.Count == 0)
        {
            return true;
        }

        foreach (var kvp in constraints)
        {
            var constraint = kvp.Value;
#if !COMPONENTS
            if (!constraint.Match(requestContext, route, kvp.Key, routeValues, routeDirection))
#else
            if (!constraint.Match(kvp.Key, routeValues))
#endif
            {
#if !COMPONENTS
                if (routeDirection.Equals(RouteDirection.IncomingRequest))
                {
                    routeValues.TryGetValue(kvp.Key, out var routeValue);

                    Log.ConstraintNotMatched(logger, routeValue!, kvp.Key, kvp.Value);
                }
#endif

                return false;
            }
        }

        return true;
    }

#if !COMPONENTS
    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug,
            "Route value '{RouteValue}' with key '{RouteKey}' did not match the constraint '{RouteConstraint}'",
            EventName = "ConstraintNotMatched")]
        public static partial void ConstraintNotMatched(
            ILogger logger,
            object routeValue,
            string routeKey,
            IRouteConstraint routeConstraint);
    }
#endif
}