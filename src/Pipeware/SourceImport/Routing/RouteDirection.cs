// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing.Abstractions/src/RouteDirection.cs
// Source Sha256: bf5629c174cc259751772142e8c001dd34ed29ea

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Routing;

/// <summary>
/// Indicates whether ASP.NET routing is processing a URL from an HTTP request or generating a URL.
/// </summary>
public enum RouteDirection
{
    /// <summary>
    /// A URL from a client is being processed.
    /// </summary>
    IncomingRequest,

    /// <summary>
    /// A URL is being created based on the route definition.
    /// </summary>
    UrlGeneration,
}