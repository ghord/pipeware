// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/Extensions/MapWhenMiddleware.cs
// Source Sha256: 8a69f3cdd8ef680e9a28f64eef6d3db7ef62e812

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;

namespace Pipeware.Builder.Extensions;

/// <summary>
/// Represents a middleware that runs a sub-request pipeline when a given predicate is matched.
/// </summary>
public class MapWhenMiddleware<TRequestContext> where TRequestContext : class, IRequestContext
{
    private readonly RequestDelegate<TRequestContext> _next;
    private readonly MapWhenOptions<TRequestContext> _options;

    /// <summary>
    /// Creates a new instance of <see cref="MapWhenMiddleware{TRequestContext}"/>.
    /// </summary>
    /// <param name="next">The delegate representing the next middleware in the request pipeline.</param>
    /// <param name="options">The middleware options.</param>
    public MapWhenMiddleware(RequestDelegate<TRequestContext> next, MapWhenOptions<TRequestContext> options)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);

        if (options.Predicate == null)
        {
            throw new ArgumentException("Predicate not set on options.", nameof(options));
        }

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

        if (_options.Predicate!(context))
        {
            return _options.Branch!(context);
        }
        return _next(context);
    }
}