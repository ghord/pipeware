// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/Matcher.cs
// Source Sha256: d392cc7e5ce858923f8cd96576462b86559af3e2

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;

namespace Pipeware.Routing.Matching;

/// <summary>
/// An interface for components that can select an <see cref="Endpoint{TRequestContext}"/> given the current request, as part
/// of the execution of <see cref="EndpointRoutingMiddleware{TRequestContext}"/>.
/// </summary>
internal abstract class Matcher<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Attempts to asynchronously select an <see cref="Endpoint{TRequestContext}"/> for the current request.
    /// </summary>
    /// <param name="httpContext">The <see cref="TRequestContext"/> associated with the current request.</param>
    /// <returns>A <see cref="Task"/> which represents the asynchronous completion of the operation.</returns>
    public abstract Task MatchAsync(TRequestContext requestContext);
}