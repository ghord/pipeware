// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/IEndpointNameMetadata.cs
// Source Sha256: 79ddfa7a66ecc01b3a450161095406a91715983d

// Originally licensed under:

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;

namespace Pipeware.Routing;

/// <summary>
/// Defines a contract use to specify an endpoint name in <see cref="Endpoint.Metadata"/>.
/// </summary>
/// <remarks>
/// Endpoint names must be unique within an application, and can be used to unambiguously
/// identify a desired endpoint for URI generation using <see cref="LinkGenerator{TRequestContext}"/>.
/// </remarks>
public interface IEndpointNameMetadata
{
    /// <summary>
    /// Gets the endpoint name.
    /// </summary>
    string EndpointName { get; }
}