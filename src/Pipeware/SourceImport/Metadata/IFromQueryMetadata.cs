// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/Metadata/IFromQueryMetadata.cs
// Source Sha256: df9ce62040e293c2233d14cdefe2be8a35c191f0

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Metadata;

/// <summary>
/// Interface marking attributes that specify a parameter should be bound using the request query string.
/// </summary>
public interface IFromQueryMetadata
{
    /// <summary>
    /// The name of the query string field.
    /// </summary>
    string? Name { get; }
}