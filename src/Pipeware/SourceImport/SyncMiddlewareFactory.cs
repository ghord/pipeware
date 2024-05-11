// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http/src/MiddlewareFactory.cs
// Source alias: sync
// Source Sha256: 41939ba2c89c5a4fa45b3b3a3250480e6ec558c5

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Pipeware;

/// <summary>
/// Default implementation for <see cref="ISyncMiddlewareFactory{TRequestContext}"/>.
/// </summary>
public class SyncMiddlewareFactory<TRequestContext> : ISyncMiddlewareFactory<TRequestContext> where TRequestContext : class, IRequestContext
{
    // The default middleware factory is just an IServiceProvider proxy.
    // This should be registered as a scoped service so that the middleware instances
    // don't end up being singletons.
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="SyncMiddlewareFactory{TRequestContext}"/>.
    /// </summary>
    /// <param name="serviceProvider">The application services.</param>
    public SyncMiddlewareFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public ISyncMiddleware<TRequestContext>? Create(Type middlewareType)
    {
        return _serviceProvider.GetRequiredService(middlewareType) as ISyncMiddleware<TRequestContext>;
    }

    /// <inheritdoc/>
    public void Release(ISyncMiddleware<TRequestContext> middleware)
    {
        // The container owns the lifetime of the service
    }
}