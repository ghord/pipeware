// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/IEndpointAddressScheme.cs
// Source Sha256: 2b0ca8e3d66cdf48b062fd00fba83f2bb3a30cb8

// Originally licensed under:

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;

namespace Pipeware.Routing;

/// <summary>
/// Defines a contract to find endpoints based on the provided address.
/// </summary>
/// <typeparam name="TAddress">The address type to look up endpoints.</typeparam>
public interface IEndpointAddressScheme<TAddress, TRequestContext> where TRequestContext : class, IRequestContext{
    /// <summary>
    /// Finds endpoints based on the provided <paramref name="address"/>.
    /// </summary>
    /// <param name="address">The information used to look up endpoints.</param>
    /// <returns>A collection of <see cref="Endpoint{TRequestContext}"/>.</returns>
    IEnumerable<Endpoint<TRequestContext>> FindEndpoints(TAddress address);
}