// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/DfaMatcherFactory.cs
// Source Sha256: 903f18324e4f71f004aa3b660438c7f85ec629ab

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Pipeware.Routing.Matching;

internal sealed class DfaMatcherFactory<TRequestContext> : MatcherFactory<TRequestContext> where TRequestContext : class, IRequestContext
{
    private readonly IServiceProvider _services;

    // Using the service provider here so we can avoid coupling to the dependencies
    // of DfaMatcherBuilder.
    public DfaMatcherFactory(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _services = services;
    }

    public override Matcher<TRequestContext> CreateMatcher(EndpointDataSource<TRequestContext> dataSource)
    {
        ArgumentNullException.ThrowIfNull(dataSource);

        // Creates a tracking entry in DI to stop listening for change events
        // when the services are disposed.
        var lifetime = _services.GetRequiredService<DataSourceDependentMatcher<TRequestContext>.Lifetime>();

        return new DataSourceDependentMatcher<TRequestContext>(dataSource, lifetime, _services.GetRequiredService<DfaMatcherBuilder<TRequestContext>>);
    }
}