// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/ISuppressLinkGenerationMetadata.cs
// Source Sha256: 3305e5e8ab5b8e052d8582bf5ad92f27735c8905

// Originally licensed under:

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Routing;

/// <summary>
/// Represents metadata used during link generation. If <see cref="SuppressLinkGeneration"/> is <c>true</c>
/// the associated endpoint will not be used for link generation.
/// </summary>
public interface ISuppressLinkGenerationMetadata
{
    /// <summary>
    /// Gets a value indicating whether the associated endpoint should be used for link generation.
    /// </summary>
    bool SuppressLinkGeneration { get; }
}