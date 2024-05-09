// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Builder/RouteHandlerBuilder.cs
// Source Sha256: 86a474189556719e885ee5fb9591f26976ab187b

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Builder;

/// <summary>
/// Builds conventions that will be used for customization of MapAction <see cref="EndpointBuilder{TRequestContext}"/> instances.
/// </summary>
public sealed class RouteHandlerBuilder<TRequestContext> : IEndpointConventionBuilder<TRequestContext>
, IEndpointConventionBuilder<TRequestContext, RouteHandlerBuilder<TRequestContext>> where TRequestContext : class, IRequestContext
{
    private readonly IEnumerable<IEndpointConventionBuilder<TRequestContext>>? _endpointConventionBuilders;
    private readonly ICollection<Action<EndpointBuilder<TRequestContext>>>? _conventions;
    private readonly ICollection<Action<EndpointBuilder<TRequestContext>>>? _finallyConventions;

    internal RouteHandlerBuilder(ICollection<Action<EndpointBuilder<TRequestContext>>> conventions, ICollection<Action<EndpointBuilder<TRequestContext>>> finallyConventions)
    {
        _conventions = conventions;
        _finallyConventions = finallyConventions;
    }

    /// <summary>
    /// Instantiates a new <see cref="RouteHandlerBuilder{TRequestContext}" /> given multiple
    /// <see cref="IEndpointConventionBuilder{TRequestContext}" /> instances.
    /// </summary>
    /// <param name="endpointConventionBuilders">A sequence of <see cref="IEndpointConventionBuilder{TRequestContext}" /> instances.</param>
    public RouteHandlerBuilder(IEnumerable<IEndpointConventionBuilder<TRequestContext>> endpointConventionBuilders)
    {
        _endpointConventionBuilders = endpointConventionBuilders;
    }

    /// <summary>
    /// Adds the specified convention to the builder. Conventions are used to customize <see cref="EndpointBuilder{TRequestContext}"/> instances.
    /// </summary>
    /// <param name="convention">The convention to add to the builder.</param>
    public void Add(Action<EndpointBuilder<TRequestContext>> convention)
    {
        if (_conventions is not null)
        {
            _conventions.Add(convention);
        }
        else
        {
            foreach (var endpointConventionBuilder in _endpointConventionBuilders!)
            {
                endpointConventionBuilder.Add(convention);
            }
        }
    }

    /// <inheritdoc />
    public void Finally(Action<EndpointBuilder<TRequestContext>> finalConvention)
    {
        if (_finallyConventions is not null)
        {
            _finallyConventions.Add(finalConvention);
        }
        else
        {
            foreach (var endpointConventionBuilder in _endpointConventionBuilders!)
            {
                endpointConventionBuilder.Finally(finalConvention);
            }
        }
    }
}