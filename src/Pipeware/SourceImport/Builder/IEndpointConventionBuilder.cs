// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/Extensions/IEndpointConventionBuilder.cs
// Source Sha256: 288151402c446574abd4efca47005c1cde70e6cd

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Builder;

/// <summary>
/// Builds conventions that will be used for customization of <see cref="EndpointBuilder{TRequestContext}"/> instances.
/// </summary>
/// <remarks>
/// This interface is used at application startup to customize endpoints for the application.
/// </remarks>
public interface IEndpointConventionBuilder<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Adds the specified convention to the builder. Conventions are used to customize <see cref="EndpointBuilder{TRequestContext}"/> instances.
    /// </summary>
    /// <param name="convention">The convention to add to the builder.</param>
    void Add(Action<EndpointBuilder<TRequestContext>> convention);

    /// <summary>
    /// Registers the specified convention for execution after conventions registered
    /// via <see cref="Add(Action{EndpointBuilder<TRequestContext>})"/>
    /// </summary>
    /// <param name="finallyConvention">The convention to add to the builder.</param>
    void Finally(Action<EndpointBuilder<TRequestContext>> finallyConvention) => throw new NotImplementedException();
}