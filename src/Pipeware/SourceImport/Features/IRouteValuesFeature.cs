// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/Routing/IRouteValuesFeature.cs
// Source Sha256: f03b818489a37e823f9454b5ed6425cab67a9765

// Originally licensed under:

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware.Routing;

namespace Pipeware.Features;

/// <summary>
/// A feature interface for routing values. Use <see cref="HttpContext.Features"/>
/// to access the values associated with the current request.
/// </summary>
public interface IRouteValuesFeature
{
    /// <summary>
    /// Gets or sets the <see cref="RouteValueDictionary"/> associated with the current
    /// request.
    /// </summary>
    RouteValueDictionary RouteValues { get; set; }
}