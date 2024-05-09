// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/Extensions/RunExtensions.cs
// Source Sha256: d0fb02958ddb0e85778d9bd0cdaf835a06368611

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;

namespace Pipeware.Builder;

/// <summary>
/// Extension methods for adding terminal middleware.
/// </summary>
public static class RunExtensions
{
    /// <summary>
    /// Adds a terminal middleware delegate to the application's request pipeline.
    /// </summary>
    /// <param name="app">The <see cref="IPipelineBuilder{TRequestContext}"/> instance.</param>
    /// <param name="handler">A delegate that handles the request.</param>
    public static void Run<TRequestContext>(this IPipelineBuilder<TRequestContext> app, RequestDelegate<TRequestContext> handler) where TRequestContext : class, IRequestContext
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(handler);

        app.Use(_ => handler);
    }
}