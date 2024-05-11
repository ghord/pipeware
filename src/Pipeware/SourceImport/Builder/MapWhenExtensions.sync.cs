// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/Extensions/MapWhenExtensions.cs
// Source alias: sync
// Source Sha256: 2fea33752ce791aa9049df84396aa15e9b160965

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware.Builder.Extensions;
using Pipeware;

namespace Pipeware.Builder;



/// <summary>
/// Extension methods for the <see cref="MapWhenSyncMiddleware{TRequestContext}"/>.
/// </summary>
public static partial class MapWhenExtensions
{
    /// <summary>
    /// Branches the request pipeline based on the result of the given predicate.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="predicate">Invoked with the request environment to determine if the branch should be taken</param>
    /// <param name="configuration">Configures a branch to take</param>
    /// <returns></returns>
    public static ISyncPipelineBuilder<TRequestContext> MapWhen<TRequestContext>(this ISyncPipelineBuilder<TRequestContext> app, Func<TRequestContext, bool> predicate, Action<ISyncPipelineBuilder<TRequestContext>> configuration) where TRequestContext : class, IRequestContext
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(configuration);

        // create branch
        var branchBuilder = app.New();
        configuration(branchBuilder);
        var branch = branchBuilder.Build();

        // put middleware in pipeline
        var options = new MapWhenSyncOptions<TRequestContext>
        {
            Predicate = predicate,
            Branch = branch,
        };
        return app.Use(next => new MapWhenSyncMiddleware<TRequestContext>(next, options).Invoke);
    }
}