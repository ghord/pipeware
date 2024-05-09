// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/Extensions/MapMiddleware.cs
// Source Sha256: 24edc1efc1c317e19d2180efa9556f877fc33c98

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;

namespace Pipeware.Builder.Extensions;

/// <summary>
/// Represents a middleware that maps a request path to a sub-request pipeline.
/// </summary>
public class MapMiddleware<TRequestContext> where TRequestContext : class, IRequestContext
{
    private readonly RequestDelegate<TRequestContext> _next;
    private readonly MapOptions<TRequestContext> _options;

    /// <summary>
    /// Creates a new instance of <see cref="MapMiddleware{TRequestContext}"/>.
    /// </summary>
    /// <param name="next">The delegate representing the next middleware in the request pipeline.</param>
    /// <param name="options">The middleware options.</param>
    public MapMiddleware(RequestDelegate<TRequestContext> next, MapOptions<TRequestContext> options)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);

        if (options.Branch == null)
        {
            throw new ArgumentException("Branch not set on options.", nameof(options));
        }

        _next = next;
        _options = options;
    }

    /// <summary>
    /// Executes the middleware.
    /// </summary>
    /// <param name="context">The <see cref="TRequestContext"/> for the current request.</param>
    /// <returns>A task that represents the execution of this middleware.</returns>
    public Task Invoke(TRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.GetRequestPathFeature().Path.StartsWithSegments(_options.PathMatch, out var matchedPath, out var remainingPath))
        {
            if (!_options.PreserveMatchedPathSegment)
            {
                return InvokeCore(context, matchedPath, remainingPath);
            }
            return _options.Branch!(context);
        }
        return _next(context);
    }

    private async Task InvokeCore(TRequestContext context, PathString matchedPath, PathString remainingPath)
    {
        var path = context.GetRequestPathFeature().Path;
        var pathBase = context.GetRequestPathFeature().PathBase;

        // Update the path
        context.GetRequestPathFeature().PathBase = pathBase.Add(matchedPath);
        context.GetRequestPathFeature().Path = remainingPath;

        try
        {
            await _options.Branch!(context);
        }
        finally
        {
            context.GetRequestPathFeature().PathBase = pathBase;
            context.GetRequestPathFeature().Path = path;
        }
    }
}