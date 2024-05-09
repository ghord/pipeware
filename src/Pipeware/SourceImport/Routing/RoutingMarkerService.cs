// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/RoutingMarkerService.cs
// Source Sha256: 6ecb681a495d605c1af910b0ab70666cf2b07de0

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Pipeware.Routing;

/// <summary>
/// A marker class used to determine if all the routing services were added
/// to the <see cref="IServiceCollection"/> before routing is configured.
/// </summary>
internal sealed class RoutingMarkerService<TRequestContext> where TRequestContext : class, IRequestContext
{
}