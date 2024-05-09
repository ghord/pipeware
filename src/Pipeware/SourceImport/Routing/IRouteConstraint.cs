// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing.Abstractions/src/IRouteConstraint.cs
// Source Sha256: 8142abdd241bf41fcb7e86d9463bbc722e393c6d

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !COMPONENTS
using Pipeware;
#else
using Microsoft.AspNetCore.Components.Routing;
#endif

namespace Pipeware.Routing;

#if !COMPONENTS
/// <summary>
/// Defines the contract that a class must implement in order to check whether a URL parameter
/// value is valid for a constraint.
/// </summary>
public interface IRouteConstraint : IParameterPolicy
#else
internal interface IRouteConstraint : IParameterPolicy
#endif
{
#if !COMPONENTS
    /// <summary>
    /// Determines whether the URL parameter contains a valid value for this constraint.
    /// </summary>
    /// <param name="httpContext">An object that encapsulates information about the HTTP request.</param>
    /// <param name="route">The router that this constraint belongs to.</param>
    /// <param name="routeKey">The name of the parameter that is being checked.</param>
    /// <param name="values">A dictionary that contains the parameters for the URL.</param>
    /// <param name="routeDirection">
    /// An object that indicates whether the constraint check is being performed
    /// when an incoming request is being handled or when a URL is being generated.
    /// </param>
    /// <returns><c>true</c> if the URL parameter contains a valid value; otherwise, <c>false</c>.</returns>
    bool Match(
        IRequestContext? requestContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection);
#else
    bool Match(
        string routeKey,
        RouteValueDictionary values);
#endif
}