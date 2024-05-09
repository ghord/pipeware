// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/Metadata/IFromBodyMetadata.cs
// Source Sha256: 70b92a892698b9438ce8d6a570a1ba2a22bf6ffe

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Metadata;

/// <summary>
/// Interface marking attributes that specify a parameter should be bound using the request body.
/// </summary>
public interface IFromBodyMetadata
{
    /// <summary>
    /// Gets whether empty input should be rejected or treated as valid.
    /// </summary>
    bool AllowEmpty => false;
}