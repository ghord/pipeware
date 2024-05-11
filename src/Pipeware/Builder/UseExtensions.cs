// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: src/Http/Http.Abstractions/src/Extensions/UseExtensions.cs

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;

namespace Pipeware.Builder;

/// <summary>
/// Extension methods for adding middleware.
/// </summary>
public static class UseExtensions
{
    /// <summary>
    /// Adds a middleware delegate defined in-line to the application's request pipeline.
    /// If you aren't calling the next function, use <see cref="RunExtensions.Run(IApplicationBuilder, RequestDelegate)"/> instead.
    /// </summary>
    /// <param name="app">The <see cref="IPipelineBuilder{TRequestContext}"/> instance.</param>
    /// <param name="middleware">A function that handles the request and calls the given next function.</param>
    /// <returns>The <see cref="IPipelineBuilder{TRequestContext}"/> instance.</returns>
    public static IPipelineBuilder<TRequestContext> Use<TRequestContext>(this IPipelineBuilder<TRequestContext> app, Func<TRequestContext, RequestDelegate<TRequestContext>, Task> middleware) where TRequestContext : class, IRequestContext
    {
        return app.Use(next => context => middleware(context, next));
    }

    /// <summary>
    /// Adds a middleware delegate defined in-line to the application's request pipeline.
    /// If you aren't calling the next function, use <see cref="RunExtensions.Run(IApplicationBuilder, RequestDelegate)"/> instead.
    /// </summary>
    /// <param name="app">The <see cref="ISyncPipelineBuilder{TRequestContext}"/> instance.</param>
    /// <param name="middleware">A function that handles the request and calls the given next function.</param>
    /// <returns>The <see cref="ISyncPipelineBuilder{TRequestContext}"/> instance.</returns>
    public static ISyncPipelineBuilder<TRequestContext> Use<TRequestContext>(this ISyncPipelineBuilder<TRequestContext> app, Action<TRequestContext, SyncRequestDelegate<TRequestContext>> middleware) where TRequestContext : class, IRequestContext
    {
        return app.Use(next => context => middleware(context, next));
    }
}