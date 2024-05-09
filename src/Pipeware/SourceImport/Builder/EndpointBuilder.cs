// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/Extensions/EndpointBuilder.cs
// Source Sha256: c767c4efe9541526dc1277dce9c4fd0663940638

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;

namespace Pipeware.Builder;

/// <summary>
/// A base class for building an new <see cref="Endpoint{TRequestContext}"/>.
/// </summary>
public abstract class EndpointBuilder<TRequestContext> where TRequestContext : class, IRequestContext
{
    private List<Func<EndpointFilterFactoryContext, EndpointFilterDelegate<TRequestContext>, EndpointFilterDelegate<TRequestContext>>>? _filterFactories;

    /// <summary>
    /// Gets the list of filters that apply to this endpoint.
    /// </summary>
    public IList<Func<EndpointFilterFactoryContext, EndpointFilterDelegate<TRequestContext>, EndpointFilterDelegate<TRequestContext>>> FilterFactories => _filterFactories ??= new();

    /// <summary>
    /// Gets or sets the delegate used to process requests for the endpoint.
    /// </summary>
    public RequestDelegate<TRequestContext>? RequestDelegate { get; set; }

    /// <summary>
    /// Gets or sets the informational display name of this endpoint.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets the collection of metadata associated with this endpoint.
    /// </summary>
    public IList<object> Metadata { get; } = new List<object>();

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> associated with the endpoint.
    /// </summary>
    public IServiceProvider ApplicationServices { get; init; } = EmptyServiceProvider.Instance;

    /// <summary>
    /// Creates an instance of <see cref="Endpoint{TRequestContext}"/> from the <see cref="EndpointBuilder{TRequestContext}"/>.
    /// </summary>
    /// <returns>The created <see cref="Endpoint{TRequestContext}"/>.</returns>
    public abstract Endpoint<TRequestContext> Build();

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new EmptyServiceProvider();
        public object? GetService(Type serviceType) => null;
    }
}