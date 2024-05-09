// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/IBindableFromHttpContextOfT.cs
// Source Sha256: 0f414b849e5f86f5fc5e942a6bea9202eb400116

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Pipeware;

/// <summary>
/// Defines a mechanism for creating an instance of a type from an <see cref="TRequestContext"/> when binding parameters for an endpoint
/// route handler delegate.
/// </summary>
/// <typeparam name="TSelf">The type that implements this interface.</typeparam>
public interface IBindableFromRequestContext<TSelf, TRequestContext> where TSelf : class, IBindableFromRequestContext<TSelf, TRequestContext>
 where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Creates an instance of <typeparamref name="TSelf"/> from the <see cref="TRequestContext"/>.
    /// </summary>
    /// <param name="context">The <see cref="TRequestContext"/> for the current request.</param>
    /// <param name="parameter">The <see cref="ParameterInfo"/> for the parameter of the route handler delegate the returned instance will populate.</param>
    /// <returns>The instance of <typeparamref name="TSelf"/>.</returns>
    static abstract ValueTask<TSelf?> BindAsync(TRequestContext context, ParameterInfo parameter);
}