// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/HttpResults/IResult.cs
// Source Sha256: 7ecc2ee139909b379b93da152a57f043dcd6c819

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware;

/// <summary>
/// Defines a contract that represents the result of an HTTP endpoint.
/// </summary>
public interface IResult<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Write an HTTP response reflecting the result.
    /// </summary>
    /// <param name="httpContext">The <see cref="TRequestContext"/> for the current request.</param>
    /// <returns>A task that represents the asynchronous execute operation.</returns>
    Task ExecuteAsync(TRequestContext requestContext);
}