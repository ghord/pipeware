// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/Metadata/IRouteDiagnosticsMetadata.cs
// Source Sha256: b1def2762869c12db9e18f760f3fe16c48f89bf2

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Metadata;

/// <summary>
/// Interface for specifing diagnostics text for a route.
/// </summary>
public interface IRouteDiagnosticsMetadata
{
    /// <summary>
    /// Gets diagnostics text for a route.
    /// </summary>
    string Route { get; }
}