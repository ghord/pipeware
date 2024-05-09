// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/RegexInlineRouteConstraintSetup.cs
// Source Sha256: 478e4834d8ac311b027944c335e3b71493d2d656

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware.Routing.Constraints;
using Pipeware.Routing;
using Microsoft.Extensions.Options;

namespace Pipeware.Routing;

internal sealed class RegexInlineRouteConstraintSetup<TRequestContext> : IConfigureOptions<RouteOptions<TRequestContext>> where TRequestContext : class, IRequestContext
{
    public void Configure(RouteOptions<TRequestContext> options)
    {
        var existingRegexConstraintType = options.TrimmerSafeConstraintMap["regex"];

        // Don't override regex constraint if it has already been overridden
        // this behavior here is just to add it back in if someone calls AddRouting(...)
        // after setting up routing with AddRoutingCore(...).
        if (existingRegexConstraintType == typeof(RegexErrorStubRouteConstraint))
        {
            options.SetParameterPolicy<RegexInlineRouteConstraint>("regex");
        }
    }
}