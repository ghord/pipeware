// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/RouteNameMetadata.cs
// Source Sha256: 0ffae02381bfd72861b8c4d19891dccd637257ad

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Pipeware.Internal;

namespace Pipeware.Routing;

/// <summary>
/// Metadata used during link generation to find the associated endpoint using route name.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class RouteNameMetadata : IRouteNameMetadata
{
    /// <summary>
    /// Creates a new instance of <see cref="RouteNameMetadata"/> with the provided route name.
    /// </summary>
    /// <param name="routeName">The route name. Can be <see langword="null"/>.</param>
    public RouteNameMetadata(string? routeName)
    {
        RouteName = routeName;
    }

    /// <summary>
    /// Gets the route name. Can be <see langword="null"/>.
    /// </summary>
    public string? RouteName { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return DebuggerHelpers.GetDebugText(nameof(RouteName), RouteName);
    }
}