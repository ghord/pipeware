// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/Routing/IEndpointFeature.cs
// Source Sha256: 64bd861f62e9c2e5045671a4b1396edd65f6de4d

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Features;

/// <summary>
/// A feature interface for endpoint routing. Use <see cref="HttpContext.Features"/>
/// to access an instance associated with the current request.
/// </summary>
public interface IEndpointFeature<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Gets or sets the selected <see cref="Http.Endpoint{TRequestContext}"/> for the current
    /// request.
    /// </summary>
    Endpoint<TRequestContext>? Endpoint { get; set; }
}