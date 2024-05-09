// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/RequestDelegateResult.cs
// Source Sha256: cadfb30efe09c624c6eab0105f4530123f112116

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware;

/// <summary>
/// The result of creating a <see cref="RequestDelegate{TRequestContext}" /> from a <see cref="Delegate" />
/// </summary>
public sealed class RequestDelegateResult<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Creates a new instance of <see cref="RequestDelegateResult{TRequestContext}"/>.
    /// </summary>
    public RequestDelegateResult(RequestDelegate<TRequestContext> requestDelegate, IReadOnlyList<object> metadata)
    {
        RequestDelegate = requestDelegate;
        EndpointMetadata = metadata;
    }

    /// <summary>
    /// Gets the <see cref="RequestDelegate{TRequestContext}" />
    /// </summary>
    public RequestDelegate<TRequestContext> RequestDelegate { get; }

    /// <summary>
    /// Gets endpoint metadata inferred from creating the <see cref="RequestDelegate{TRequestContext}" />. If a non-null
    /// RequestDelegateFactoryOptions.EndpointMetadata list was passed in, this will be the same instance.
    /// </summary>
    public IReadOnlyList<object> EndpointMetadata { get; }
}