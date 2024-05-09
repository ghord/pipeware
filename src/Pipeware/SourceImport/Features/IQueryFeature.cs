// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Features/src/IQueryFeature.cs
// Source Sha256: c2186fc5a5a5f7d46b87f70d04793804b05c1633

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Features;

/// <summary>
/// Provides access to the <see cref="IQueryCollection"/> associated with the HTTP request.
/// </summary>
public interface IQueryFeature
{
    /// <summary>
    /// Gets or sets the <see cref="IQueryCollection"/>.
    /// </summary>
    IQueryCollection Query { get; set; }
}