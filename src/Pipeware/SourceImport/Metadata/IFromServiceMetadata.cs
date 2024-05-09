// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/Metadata/IFromServiceMetadata.cs
// Source Sha256: c74fcd5997a7f01fa2616a796c691dda84c92e57

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Metadata;

/// <summary>
/// Interface marking attributes that specify a parameter should be bound using request services.
/// </summary>
public interface IFromServiceMetadata
{
}