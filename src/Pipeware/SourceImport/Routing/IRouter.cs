// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing.Abstractions/src/IRouter.cs
// Source Sha256: ac79858f2d636d9349b513a5107d18ffd90600c5

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Routing;

/// <summary>
/// Interface for implementing a router.
/// </summary>
public interface IRouter
{
    /// <summary>
    /// Asynchronously routes based on the current <paramref name="context"/>.
    /// </summary>
    /// <param name="context">A <see cref="RouteContext"/> instance.</param>
    Task RouteAsync(RouteContext context);

    /// <summary>
    /// Returns the URL that is associated with the route details provided in <paramref name="context"/>
    /// </summary>
    /// <param name="context">A <see cref="VirtualPathContext"/> instance.</param>
    /// <returns>A <see cref="VirtualPathData"/> object. Can be null.</returns>
    VirtualPathData? GetVirtualPath(VirtualPathContext context);
}