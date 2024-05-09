// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/ISuppressMatchingMetadata.cs
// Source Sha256: ca0a79e8441b97a42c116656bf5d52574b48567a

// Originally licensed under:

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Routing;

/// <summary>
/// Metadata used to prevent URL matching. If <see cref="SuppressMatching"/> is <c>true</c> the
/// associated endpoint will not be considered for URL matching.
/// </summary>
public interface ISuppressMatchingMetadata
{
    /// <summary>
    /// Gets a value indicating whether the associated endpoint should be used for URL matching.
    /// </summary>
    bool SuppressMatching { get; }
}