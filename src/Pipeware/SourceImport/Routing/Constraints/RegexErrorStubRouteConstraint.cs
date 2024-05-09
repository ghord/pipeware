// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Constraints/RegexErrorStubRouteConstraint.cs
// Source Sha256: aeb0324b05960715cd260ac78eda7b324f1ef4e5

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;

namespace Pipeware.Routing.Constraints;

internal sealed class RegexErrorStubRouteConstraint : IRouteConstraint
{
    public RegexErrorStubRouteConstraint(string _)
    {
        throw new InvalidOperationException("A route parameter uses the regex constraint, which isn't registered. If this application was configured using CreateSlimBuilder(...) or AddRoutingCore(...) then this constraint is not registered by default. To use the regex constraint, configure route options at app startup: services.Configure<RouteOptions>(options => options.SetParameterPolicy<RegexInlineRouteConstraint>(\"regex\"));");
    }

    bool IRouteConstraint.Match(IRequestContext? requestContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection)
    {
        // Should never get called, but is same as throw in constructor in case constructor is changed.
        throw new InvalidOperationException("A route parameter uses the regex constraint, which isn't registered. If this application was configured using CreateSlimBuilder(...) or AddRoutingCore(...) then this constraint is not registered by default. To use the regex constraint, configure route options at app startup: services.Configure<RouteOptions>(options => options.SetParameterPolicy<RegexInlineRouteConstraint>(\"regex\"));");
    }
}