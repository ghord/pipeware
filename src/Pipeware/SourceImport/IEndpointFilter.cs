// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/IEndpointFilter.cs
// Source Sha256: 7cd316ab00b571828cf780124ec83923928f1bc1

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware;

/// <summary>
/// Provides an interface for implementing a filter targetting a route handler.
/// </summary>
public interface IEndpointFilter<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Implements the core logic associated with the filter given a <see cref="EndpointFilterInvocationContext{TRequestContext}"/>
    /// and the next filter to call in the pipeline.
    /// </summary>
    /// <param name="context">The <see cref="EndpointFilterInvocationContext{TRequestContext}"/> associated with the current request/response.</param>
    /// <param name="next">The next filter in the pipeline.</param>
    /// <returns>An awaitable result of calling the handler and apply
    /// any modifications made by filters in the pipeline.</returns>
    ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext<TRequestContext> context, EndpointFilterDelegate<TRequestContext> next);
}