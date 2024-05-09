// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/EndpointNameMetadata.cs
// Source Sha256: 41428703bfcc18cbb81430631d0843554259b1a5

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Pipeware;
using Pipeware.Internal;

namespace Pipeware.Routing;

/// <summary>
/// Specifies an endpoint name in <see cref="Endpoint.Metadata"/>.
/// </summary>
/// <remarks>
/// Endpoint names must be unique within an application, and can be used to unambiguously
/// identify a desired endpoint for URI generation using <see cref="LinkGenerator{TRequestContext}"/>.
/// </remarks>
[DebuggerDisplay("{ToString(),nq}")]
public class EndpointNameMetadata : IEndpointNameMetadata
{
    /// <summary>
    /// Creates a new instance of <see cref="EndpointNameMetadata"/> with the provided endpoint name.
    /// </summary>
    /// <param name="endpointName">The endpoint name.</param>
    public EndpointNameMetadata(string endpointName)
    {
        ArgumentNullException.ThrowIfNull(endpointName);

        EndpointName = endpointName;
    }

    /// <summary>
    /// Gets the endpoint name.
    /// </summary>
    public string EndpointName { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        return DebuggerHelpers.GetDebugText(nameof(EndpointName), EndpointName);
    }
}