// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Features/src/IHttpRequestLifetimeFeature.cs
// Source Sha256: f51b5db80903a46d3a4c693b81c5c8051a5cab3c

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Features;

/// <summary>
/// Provides access to the HTTP request lifetime operations.
/// </summary>
public interface IRequestLifetimeFeature
{
    /// <summary>
    /// A <see cref="CancellationToken"/> that fires if the request is aborted and
    /// the application should cease processing. The token will not fire if the request
    /// completes successfully.
    /// </summary>
    CancellationToken RequestAborted { get; set; }

    /// <summary>
    /// Forcefully aborts the request if it has not already completed. This will result in
    /// RequestAborted being triggered.
    /// </summary>
    void Abort();
}