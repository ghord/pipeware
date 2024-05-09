// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/IEndpointGroupNameMetadata.cs
// Source Sha256: 108d0f2a9530bc58e8a6bb750c9f5eabe9a18374

// Originally licensed under:

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;

namespace Pipeware.Routing;

/// <summary>
/// Defines a contract used to specify an endpoint group name in <see cref="Endpoint.Metadata"/>.
/// </summary>
public interface IEndpointGroupNameMetadata
{
    /// <summary>
    /// Gets the endpoint group name.
    /// </summary>
    string EndpointGroupName { get; }
}