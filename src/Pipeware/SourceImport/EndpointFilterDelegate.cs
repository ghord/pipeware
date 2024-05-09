// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/EndpointFilterDelegate.cs
// Source Sha256: d473cd52e7c7506feab8320eb7684af1fd4e4b19

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware;

/// <summary>
/// A delegate that is applied as a filter on a route handler.
/// </summary>
/// <param name="context">The <see cref="EndpointFilterInvocationContext{TRequestContext}"/> associated with the current request.</param>
/// <returns>
/// A <see cref="ValueTask"/> result of calling the handler and applying any modifications made by filters in the pipeline.
/// </returns>
public delegate ValueTask<object?> EndpointFilterDelegate<TRequestContext>(EndpointFilterInvocationContext<TRequestContext> context) where TRequestContext : class, IRequestContext;