// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Constraints/NullRouteConstraint.cs
// Source Sha256: c025bec3f7ed65cd44e7e2195df092a2c96fe909

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !COMPONENTS
using Pipeware;
#else
using Microsoft.AspNetCore.Components.Routing;
#endif

namespace Pipeware.Routing.Constraints;

internal sealed class NullRouteConstraint : IRouteConstraint
{
    public static readonly NullRouteConstraint Instance = new NullRouteConstraint();

    private NullRouteConstraint()
    {
    }

#if !COMPONENTS
    public bool Match(IRequestContext? requestContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
#else
    public bool Match(string routeKey, RouteValueDictionary values)
#endif
    {
        return true;
    }
}