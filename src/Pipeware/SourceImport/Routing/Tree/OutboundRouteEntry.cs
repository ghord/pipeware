// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Tree/OutboundRouteEntry.cs
// Source Sha256: f33c266ceb96ac391ee973a7fecb48cb912c8822

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Pipeware.Routing.Template;

namespace Pipeware.Routing.Tree;

/// <summary>
/// Used to build a <see cref="TreeRouter"/>. Represents a URL template that will be used to generate
/// outgoing URLs.
/// </summary>
public class OutboundRouteEntry
{
    /// <summary>
    /// Gets or sets the route constraints.
    /// </summary>
    public IDictionary<string, IRouteConstraint> Constraints { get; set; }

    /// <summary>
    /// Gets or sets the route defaults.
    /// </summary>
    public RouteValueDictionary Defaults { get; set; }

    /// <summary>
    /// The <see cref="IRouter"/> to invoke when this entry matches.
    /// </summary>
    public IRouter Handler { get; set; }

    /// <summary>
    /// Gets or sets the order of the entry.
    /// </summary>
    /// <remarks>
    /// Entries are ordered first by <see cref="Order"/> (ascending) then by <see cref="Precedence"/> (descending).
    /// </remarks>
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets the precedence of the template for link generation. A greater value of
    /// <see cref="Precedence"/> means that an entry is considered first.
    /// </summary>
    /// <remarks>
    /// Entries are ordered first by <see cref="Order"/> (ascending) then by <see cref="Precedence"/> (descending).
    /// </remarks>
    public decimal Precedence { get; set; }

    /// <summary>
    /// Gets or sets the name of the route.
    /// </summary>
    public string RouteName { get; set; }

    /// <summary>
    /// Gets or sets the set of values that must be present for link genration.
    /// </summary>
    public RouteValueDictionary RequiredLinkValues { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="RouteTemplate"/>.
    /// </summary>
    public RouteTemplate RouteTemplate { get; set; }

    /// <summary>
    /// Gets or sets the data that is associated with this entry.
    /// </summary>
    public object Data { get; set; }
}