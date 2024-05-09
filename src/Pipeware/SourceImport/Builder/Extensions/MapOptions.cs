// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/Extensions/MapOptions.cs
// Source Sha256: 16afae0b0c2496061d53ea887f8cc03d8950ccf5

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;

namespace Pipeware.Builder.Extensions;

/// <summary>
/// Options for the <see cref="MapMiddleware{TRequestContext}"/>.
/// </summary>
public class MapOptions<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// The path to match.
    /// </summary>
    public PathString PathMatch { get; set; }

    /// <summary>
    /// The branch taken for a positive match.
    /// </summary>
    public RequestDelegate<TRequestContext>? Branch { get; set; }

    /// <summary>
    /// If false, matched path would be removed from Request.Path and added to Request.PathBase
    /// Defaults to false.
    /// </summary>
    public bool PreserveMatchedPathSegment { get; set; }
}