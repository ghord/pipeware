// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/RouteHandlerOptions.cs
// Source Sha256: 4fb2106e17a002dc9722a76f3c8d7c21eb959b32

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware.Builder;
using Pipeware;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Pipeware.Routing;

/// <summary>
/// Options for controlling the behavior of <see cref="EndpointRouteBuilderExtensions.MapGet(IEndpointRouteBuilder, string, Delegate)"/>
/// and similar methods.
/// </summary>
public sealed class RouteHandlerOptions
{
    /// <summary>
    /// Controls whether endpoints should throw a <see cref="BadHttpRequestException"/> in addition to
    /// writing a <see cref="LogLevel.Debug"/> log when handling invalid requests.
    /// </summary>
    /// <remarks>
    /// Defaults to <see cref="HostEnvironmentEnvExtensions.IsDevelopment(IHostEnvironment)"/>.
    /// </remarks>
    public bool ThrowOnBadRequest { get; set; }
}