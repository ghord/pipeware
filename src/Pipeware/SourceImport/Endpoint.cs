// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/Routing/Endpoint.cs
// Source Sha256: b418ce57b0e19c8ca182a748ff733b45dab497b5

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Pipeware;

/// <summary>
/// Represents a logical endpoint in an application.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public class Endpoint<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Creates a new instance of <see cref="Endpoint{TRequestContext}"/>.
    /// </summary>
    /// <param name="requestDelegate">The delegate used to process requests for the endpoint.</param>
    /// <param name="metadata">
    /// The endpoint <see cref="EndpointMetadataCollection"/>. May be null.
    /// </param>
    /// <param name="displayName">
    /// The informational display name of the endpoint. May be null.
    /// </param>
    public Endpoint(
        RequestDelegate<TRequestContext>? requestDelegate,
        EndpointMetadataCollection? metadata,
        string? displayName)
    {
        // All are allowed to be null
        RequestDelegate = requestDelegate;
        Metadata = metadata ?? EndpointMetadataCollection.Empty;
        DisplayName = displayName;
    }

    /// <summary>
    /// Gets the informational display name of this endpoint.
    /// </summary>
    public string? DisplayName { get; }

    /// <summary>
    /// Gets the collection of metadata associated with this endpoint.
    /// </summary>
    public EndpointMetadataCollection Metadata { get; }

    /// <summary>
    /// Gets the delegate used to process requests for the endpoint.
    /// </summary>
    public RequestDelegate<TRequestContext>? RequestDelegate { get; }

    /// <summary>
    /// Returns a string representation of the endpoint.
    /// </summary>
    public override string? ToString() => DisplayName ?? base.ToString();
}