// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/IApplicationBuilder.cs
// Source alias: sync
// Source Sha256: 4d35df40f48f33d6e5c7ebe15cb78eddc40a2c1b

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;
using Pipeware.Features;

namespace Pipeware.Builder;

/// <summary>
/// Defines a class that provides the mechanisms to configure an application's request pipeline.
/// </summary>
public interface ISyncPipelineBuilder<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Gets or sets the <see cref="IServiceProvider"/> that provides access to the application's service container.
    /// </summary>
    IServiceProvider ApplicationServices { get; set; }

    /// <summary>
    /// Gets the set of HTTP features the application's server provides.
    /// </summary>
    /// <remarks>
    /// An empty collection is returned if a server wasn't specified for the application builder.
    /// </remarks>
    IFeatureCollection PipelineFeatures { get; }

    /// <summary>
    /// Gets a key/value collection that can be used to share data between middleware.
    /// </summary>
    IDictionary<string, object?> Properties { get; }

    /// <summary>
    /// Adds a middleware delegate to the application's request pipeline.
    /// </summary>
    /// <param name="middleware">The middleware delegate.</param>
    /// <returns>The <see cref="ISyncPipelineBuilder{TRequestContext}"/>.</returns>
    ISyncPipelineBuilder<TRequestContext> Use(Func<SyncRequestDelegate<TRequestContext>, SyncRequestDelegate<TRequestContext>> middleware);

    /// <summary>
    /// Creates a new <see cref="ISyncPipelineBuilder{TRequestContext}"/> that shares the <see cref="Properties"/> of this
    /// <see cref="ISyncPipelineBuilder{TRequestContext}"/>.
    /// </summary>
    /// <returns>The new <see cref="ISyncPipelineBuilder{TRequestContext}"/>.</returns>
    ISyncPipelineBuilder<TRequestContext> New();

    /// <summary>
    /// Builds the delegate used by this application to process HTTP requests.
    /// </summary>
    /// <returns>The request handling delegate.</returns>
    SyncRequestDelegate<TRequestContext> Build();
}