// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Tree/InboundRouteEntry.cs
// Source Sha256: ee8e59c72e0022dbe08a46eb70f8a765aeac27fb

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

#if COMPONENTS
using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
#else
using Pipeware.Routing.Template;
#endif

namespace Pipeware.Routing.Tree;

#if !COMPONENTS
/// <summary>
/// Used to build an <see cref="TreeRouter"/>. Represents a URL template tha will be used to match incoming
/// request URLs.
/// </summary>
public class InboundRouteEntry
#else
internal class InboundRouteEntry
#endif
{
    /// <summary>
    /// Gets or sets the route constraints.
    /// </summary>
    public IDictionary<string, IRouteConstraint> Constraints { get; set; }

    /// <summary>
    /// Gets or sets the route defaults.
    /// </summary>
    public RouteValueDictionary Defaults { get; set; }

#if !COMPONENTS
    /// <summary>
    /// Gets or sets the <see cref="IRouter"/> to invoke when this entry matches.
    /// </summary>
    public IRouter Handler { get; set; }
#else
    [DynamicallyAccessedMembers(Component)]
    public Type Handler { get; set; }
#endif

    /// <summary>
    /// Gets or sets the order of the entry.
    /// </summary>
    /// <remarks>
    /// Entries are ordered first by <see cref="Order"/> (ascending) then by <see cref="Precedence"/> (descending).
    /// </remarks>
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets the precedence of the entry.
    /// </summary>
    /// <remarks>
    /// Entries are ordered first by <see cref="Order"/> (ascending) then by <see cref="Precedence"/> (descending).
    /// </remarks>
    public decimal Precedence { get; set; }

    /// <summary>
    /// Gets or sets the name of the route.
    /// </summary>
    public string RouteName { get; set; }

#if !COMPONENTS
    /// <summary>
    /// Gets or sets the <see cref="RouteTemplate"/>.
    /// </summary>
    public RouteTemplate RouteTemplate { get; set; }
#else
    public RoutePattern RoutePattern { get; set; }

    public List<string> UnusedRouteParameterNames { get; set; }
#endif
}