// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/IEndpointRouteBuilder.cs
// Source Sha256: 7de1af9a46078db48d39ee713dc0a904af016817

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware.Builder;

namespace Pipeware.Routing;

/// <summary>
/// Defines a contract for a route builder in an application. A route builder specifies the routes for
/// an application.
/// </summary>
public interface IEndpointRouteBuilder<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Creates a new <see cref="IPipelineBuilder{TRequestContext}"/>.
    /// </summary>
    /// <returns>The new <see cref="IPipelineBuilder{TRequestContext}"/>.</returns>
    IPipelineBuilder<TRequestContext> CreateApplicationBuilder();

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> used to resolve services for routes.
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets the endpoint data sources configured in the builder.
    /// </summary>
    ICollection<EndpointDataSource<TRequestContext>> DataSources { get; }
}