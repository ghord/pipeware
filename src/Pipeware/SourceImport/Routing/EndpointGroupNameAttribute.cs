// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/EndpointGroupNameAttribute.cs
// Source Sha256: 9bf1d9003e29a2c9e3efb9dfdad02b19dafad2a2

// Originally licensed under:

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Routing;

/// <summary>
/// Specifies the endpoint group name in <see cref="Microsoft.AspNetCore.Http.Endpoint.Metadata"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Delegate | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class EndpointGroupNameAttribute : Attribute, IEndpointGroupNameMetadata
{
    /// <summary>
    /// Initializes an instance of the <see cref="EndpointGroupNameAttribute"/>.
    /// </summary>
    /// <param name="endpointGroupName">The endpoint group name.</param>
    public EndpointGroupNameAttribute(string endpointGroupName)
    {
        ArgumentNullException.ThrowIfNull(endpointGroupName);

        EndpointGroupName = endpointGroupName;
    }

    /// <inheritdoc />
    public string EndpointGroupName { get; }
}