// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/ConfigureRouteOptions.cs
// Source Sha256: b3a3892ceb0cefeeb0c50c689af843881d083b96

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware.Routing;
using Microsoft.Extensions.Options;

namespace Pipeware.Routing;

internal sealed class ConfigureRouteOptions<TRequestContext> : IConfigureOptions<RouteOptions<TRequestContext>> where TRequestContext : class, IRequestContext
{
    private readonly ICollection<EndpointDataSource<TRequestContext>> _dataSources;

    public ConfigureRouteOptions(ICollection<EndpointDataSource<TRequestContext>> dataSources)
    {
        ArgumentNullException.ThrowIfNull(dataSources);

        _dataSources = dataSources;
    }

    public void Configure(RouteOptions<TRequestContext> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.EndpointDataSources = _dataSources;
    }
}