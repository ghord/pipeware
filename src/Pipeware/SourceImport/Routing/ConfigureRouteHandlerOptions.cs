// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/ConfigureRouteHandlerOptions.cs
// Source Sha256: 5b38606d58e7d02145f1ceef39572c145b58927e

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Pipeware.Routing;

internal sealed class ConfigureRouteHandlerOptions : IConfigureOptions<RouteHandlerOptions>
{
    private readonly IHostEnvironment? _environment;

    public ConfigureRouteHandlerOptions(IHostEnvironment? environment = null)
    {
        _environment = environment;
    }

    public void Configure(RouteHandlerOptions options)
    {
        if (_environment?.IsDevelopment() ?? false)
        {
            options.ThrowOnBadRequest = true;
        }
    }
}