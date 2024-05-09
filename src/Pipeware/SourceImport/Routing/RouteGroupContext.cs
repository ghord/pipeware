// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/RouteGroupContext.cs
// Source Sha256: 558820c0e37b134bb0f375358c3aaddebe8fdfa0

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware.Builder;
using Pipeware.Routing.Patterns;

namespace Pipeware.Routing;

/// <summary>
/// Represents the information accessible to <see cref="EndpointDataSource.GetGroupedEndpoints(RouteGroupContext)"/>.
/// </summary>
public sealed class RouteGroupContext<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Gets the <see cref="RouteEndpoint.RoutePattern"/> which should prefix the <see cref="RouteEndpoint.RoutePattern"/> of all <see cref="RouteEndpoint{TRequestContext}"/> instances
    /// returned by the call to <see cref="EndpointDataSource.GetGroupedEndpoints(RouteGroupContext)"/>. This accounts for nested groups and gives the full group prefix
    /// not just the prefix supplied to the innermost call to <see cref="EndpointRouteBuilderExtensions.MapGroup(IEndpointRouteBuilder, RoutePattern)"/>.
    /// </summary>
    public required RoutePattern Prefix { get; init; }

    /// <summary>
    /// Gets all conventions added to ancestor <see cref="RouteGroupBuilder{TRequestContext}"/> instances returned from <see cref="EndpointRouteBuilderExtensions.MapGroup(IEndpointRouteBuilder, RoutePattern)"/>
    /// via <see cref="IEndpointConventionBuilder.Add(Action{EndpointBuilder<TRequestContext>})"/>. These should be applied in order when building every <see cref="RouteEndpoint{TRequestContext}"/>
    /// returned from <see cref="EndpointDataSource.GetGroupedEndpoints(RouteGroupContext)"/>.
    /// </summary>
    public IReadOnlyList<Action<EndpointBuilder<TRequestContext>>> Conventions { get; init; } = Array.Empty<Action<EndpointBuilder<TRequestContext>>>();

    /// <summary>
    /// Gets all conventions added to ancestor <see cref="RouteGroupBuilder{TRequestContext}"/> instances returned from <see cref="EndpointRouteBuilderExtensions.MapGroup(IEndpointRouteBuilder, RoutePattern)"/>
    /// via <see cref="IEndpointConventionBuilder.Add(Action{EndpointBuilder<TRequestContext>})"/>. These should be applied in LIFO order when building every <see cref="RouteEndpoint{TRequestContext}"/>
    /// returned from <see cref="EndpointDataSource.GetGroupedEndpoints(RouteGroupContext)"/>.
    /// </summary>
    public IReadOnlyList<Action<EndpointBuilder<TRequestContext>>> FinallyConventions { get; init; } = Array.Empty<Action<EndpointBuilder<TRequestContext>>>();

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