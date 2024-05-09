// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/IMiddleware.cs
// Source Sha256: fea0afa84d5a84ff7a81982bf0bb0bebf77358b9

// Originally licensed under:

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware;

/// <summary>
/// Defines middleware that can be added to the application's request pipeline.
/// </summary>
public interface IMiddleware<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Request handling method.
    /// </summary>
    /// <param name="context">The <see cref="TRequestContext"/> for the current request.</param>
    /// <param name="next">The delegate representing the remaining middleware in the request pipeline.</param>
    /// <returns>A <see cref="Task"/> that represents the execution of this middleware.</returns>
    Task InvokeAsync(TRequestContext context, RequestDelegate<TRequestContext> next);
}