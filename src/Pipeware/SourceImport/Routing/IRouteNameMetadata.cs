// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/IRouteNameMetadata.cs
// Source Sha256: 9a617cc3dab0b9a9b77de20e0147b95b89ffd26b

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Routing;

/// <summary>
/// Represents metadata used during link generation to find
/// the associated endpoint using route name.
/// </summary>
public interface IRouteNameMetadata
{
    /// <summary>
    /// Gets the route name. Can be <see langword="null"/>.
    /// </summary>
    string? RouteName { get; }
}