// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/EndpointSelector.cs
// Source Sha256: 2c64d143992e7f358ea7e557dcffc8cbcffc256c

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;

namespace Pipeware.Routing.Matching;

/// <summary>
/// A service that is responsible for the final <see cref="Endpoint{TRequestContext}"/> selection
/// decision. To use a custom <see cref="EndpointSelector{TRequestContext}"/> register an implementation
/// of <see cref="EndpointSelector{TRequestContext}"/> in the dependency injection container as a singleton.
/// </summary>
public abstract class EndpointSelector<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Asynchronously selects an <see cref="Endpoint{TRequestContext}"/> from the <see cref="CandidateSet{TRequestContext}"/>.
    /// </summary>
    /// <param name="httpContext">The <see cref="TRequestContext"/> associated with the current request.</param>
    /// <param name="candidates">The <see cref="CandidateSet{TRequestContext}"/>.</param>
    /// <returns>A <see cref="Task"/> that completes asynchronously once endpoint selection is complete.</returns>
    /// <remarks>
    /// An <see cref="EndpointSelector{TRequestContext}"/> should assign the endpoint by calling
    /// <see cref="EndpointHttpContextExtensions.SetEndpoint(HttpContext, Endpoint)"/>
    /// and setting <see cref="HttpRequest.RouteValues"/> once an endpoint is selected.
    /// </remarks>
    public abstract Task SelectAsync(TRequestContext requestContext, CandidateSet<TRequestContext> candidates);
}