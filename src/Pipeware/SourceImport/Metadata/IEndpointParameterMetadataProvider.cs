// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/Metadata/IEndpointParameterMetadataProvider.cs
// Source Sha256: 2f9a45dac57d45bc92edb476213b57ebbacc4449

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Pipeware.Builder;

namespace Pipeware.Metadata;

/// <summary>
/// Indicates that a type provides a static method that provides <see cref="Endpoint{TRequestContext}"/> metadata when declared as the
/// parameter type of an <see cref="Endpoint{TRequestContext}"/> route handler delegate.
/// </summary>
public interface IEndpointParameterMetadataProvider<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Populates metadata for the related <see cref="Endpoint{TRequestContext}"/> and <see cref="ParameterInfo"/>.
    /// </summary>
    /// <remarks>
    /// This method is called by RequestDelegateFactory when creating a <see cref="RequestDelegate{TRequestContext}"/> and by MVC when creating endpoints for controller actions.
    /// This is called for each parameter of the route handler or action with a declared type implementing this interface.
    /// Add or remove objects on the <see cref="EndpointBuilder.Metadata"/> property of the <paramref name="builder"/> to modify the <see cref="Endpoint.Metadata"/> being built.
    /// </remarks>
    /// <param name="parameter">The <see cref="ParameterInfo"/> of the route handler delegate or MVC Action of the endpoint being created.</param>
    /// <param name="builder">The <see cref="EndpointBuilder{TRequestContext}"/> used to construct the endpoint for the given <paramref name="parameter"/>.</param>
    static abstract void PopulateMetadata(ParameterInfo parameter, EndpointBuilder<TRequestContext> builder);
}