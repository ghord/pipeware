// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/EndpointFilterFactoryContext.cs
// Source Sha256: 72e2d88f1727d19792ea80c51515dae0ee515a58

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Pipeware;

/// <summary>
/// Represents the information accessible via the route handler filter
/// API when the user is constructing a new route handler.
/// </summary>
public sealed class EndpointFilterFactoryContext
{
    /// <summary>
    /// The <see cref="MethodInfo"/> associated with the current route handler, <see cref="RequestDelegate{TRequestContext}"/> or MVC action.
    /// </summary>
    /// <remarks>
    /// In the future this could support more endpoint types.
    /// </remarks>
    public required MethodInfo MethodInfo { get; init; }

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance used to access application services.
    /// </summary>
    public IServiceProvider ApplicationServices { get; init; } = EmptyServiceProvider.Instance;

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new EmptyServiceProvider();
        public object? GetService(Type serviceType) => null;
    }
}