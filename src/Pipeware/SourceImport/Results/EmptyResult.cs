// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/HttpResults/EmptyHttpResult.cs
// Source Sha256: 459a17a22c153f7239140f7b8b340d2d9ec51bfe

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Results;

/// <summary>
/// Represents an <see cref="IResult{TRequestContext}"/> that when executed will
/// do nothing.
/// </summary>
public sealed class EmptyResult<TRequestContext> : IResult<TRequestContext> where TRequestContext : class, IRequestContext
{
    private EmptyResult()
    {
    }

    /// <summary>
    /// Gets an instance of <see cref="EmptyResult{TRequestContext}"/>.
    /// </summary>
    public static EmptyResult<TRequestContext> Instance { get; } = new();

    /// <inheritdoc/>
    public Task ExecuteAsync(TRequestContext requestContext)
    {
        ArgumentNullException.ThrowIfNull(requestContext);

        return Task.CompletedTask;
    }
}