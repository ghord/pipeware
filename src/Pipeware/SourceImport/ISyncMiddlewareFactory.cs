// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/IMiddlewareFactory.cs
// Source alias: sync
// Source Sha256: 4e39bbaf868403416ee4d9e2b0319e94a4cc119f

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware;

/// <summary>
/// Provides methods to create middleware.
/// </summary>
public interface ISyncMiddlewareFactory<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Creates a middleware instance for each request.
    /// </summary>
    /// <param name="middlewareType">The concrete <see cref="Type"/> of the <see cref="ISyncMiddleware{TRequestContext}"/>.</param>
    /// <returns>The <see cref="ISyncMiddleware{TRequestContext}"/> instance.</returns>
    ISyncMiddleware<TRequestContext>? Create(Type middlewareType);

    /// <summary>
    /// Releases a <see cref="ISyncMiddleware{TRequestContext}"/> instance at the end of each request.
    /// </summary>
    /// <param name="middleware">The <see cref="ISyncMiddleware{TRequestContext}"/> instance to release.</param>
    void Release(ISyncMiddleware<TRequestContext> middleware);
}