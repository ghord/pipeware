// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/Extensions/MapExtensions.cs
// Source Sha256: 0f990846468cd67dfa05783d9f19d3729b939322

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware.Builder.Extensions;
using Pipeware;

namespace Pipeware.Builder;

/// <summary>
/// Extension methods for the <see cref="MapMiddleware{TRequestContext}"/>.
/// </summary>
public static class MapExtensions
{
    /// <summary>
    /// Branches the request pipeline based on matches of the given request path. If the request path starts with
    /// the given path, the branch is executed.
    /// </summary>
    /// <param name="app">The <see cref="IPipelineBuilder{TRequestContext}"/> instance.</param>
    /// <param name="pathMatch">The request path to match.</param>
    /// <param name="configuration">The branch to take for positive path matches.</param>
    /// <returns>The <see cref="IPipelineBuilder{TRequestContext}"/> instance.</returns>
    public static IPipelineBuilder<TRequestContext> Map<TRequestContext>(this IPipelineBuilder<TRequestContext> app, string pathMatch, Action<IPipelineBuilder<TRequestContext>> configuration) where TRequestContext : class, IRequestContext
    {
        return Map<TRequestContext>(app, pathMatch, preserveMatchedPathSegment: false, configuration);
    }

    /// <summary>
    /// Branches the request pipeline based on matches of the given request path. If the request path starts with
    /// the given path, the branch is executed.
    /// </summary>
    /// <param name="app">The <see cref="IPipelineBuilder{TRequestContext}"/> instance.</param>
    /// <param name="pathMatch">The request path to match.</param>
    /// <param name="configuration">The branch to take for positive path matches.</param>
    /// <returns>The <see cref="IPipelineBuilder{TRequestContext}"/> instance.</returns>
    public static IPipelineBuilder<TRequestContext> Map<TRequestContext>(this IPipelineBuilder<TRequestContext> app, PathString pathMatch, Action<IPipelineBuilder<TRequestContext>> configuration) where TRequestContext : class, IRequestContext
    {
        return Map<TRequestContext>(app, pathMatch, preserveMatchedPathSegment: false, configuration);
    }

    /// <summary>
    /// Branches the request pipeline based on matches of the given request path. If the request path starts with
    /// the given path, the branch is executed.
    /// </summary>
    /// <param name="app">The <see cref="IPipelineBuilder{TRequestContext}"/> instance.</param>
    /// <param name="pathMatch">The request path to match.</param>
    /// <param name="preserveMatchedPathSegment">if false, matched path would be removed from Request.Path and added to Request.PathBase.</param>
    /// <param name="configuration">The branch to take for positive path matches.</param>
    /// <returns>The <see cref="IPipelineBuilder{TRequestContext}"/> instance.</returns>
    public static IPipelineBuilder<TRequestContext> Map<TRequestContext>(this IPipelineBuilder<TRequestContext> app, PathString pathMatch, bool preserveMatchedPathSegment, Action<IPipelineBuilder<TRequestContext>> configuration) where TRequestContext : class, IRequestContext
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(configuration);

        if (pathMatch.HasValue && pathMatch.Value!.EndsWith('/'))
        {
            throw new ArgumentException("The path must not end with a '/'", nameof(pathMatch));
        }

        // create branch
        var branchBuilder = app.New();
        configuration(branchBuilder);
        var branch = branchBuilder.Build();

        var options = new MapOptions<TRequestContext>
        {
            Branch = branch,
            PathMatch = pathMatch,
            PreserveMatchedPathSegment = preserveMatchedPathSegment
        };
        return app.Use(next => new MapMiddleware<TRequestContext>(next, options).Invoke);
    }
}