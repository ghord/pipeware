// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/Metadata/IFromRouteMetadata.cs
// Source Sha256: ab53cbd77c89527890dca6603426fe200fe98363

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Metadata;

/// <summary>
/// Interface marking attributes that specify a parameter should be bound using route-data from the current request.
/// </summary>
public interface IFromRouteMetadata
{
    /// <summary>
    /// The <see cref="HttpRequest.RouteValues"/> name.
    /// </summary>
    string? Name { get; }
}