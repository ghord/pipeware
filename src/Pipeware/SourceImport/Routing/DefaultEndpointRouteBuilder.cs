// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/DefaultEndpointRouteBuilder.cs
// Source Sha256: da0ab8c21edc17a709851ee1c4bd9a13fde4774d

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware.Builder;

namespace Pipeware.Routing;

internal sealed class DefaultEndpointRouteBuilder<TRequestContext> : IEndpointRouteBuilder<TRequestContext> where TRequestContext : class, IRequestContext
{
    public DefaultEndpointRouteBuilder(IPipelineBuilder<TRequestContext> applicationBuilder)
    {
        ApplicationBuilder = applicationBuilder ?? throw new ArgumentNullException(nameof(applicationBuilder));
        DataSources = new List<EndpointDataSource<TRequestContext>>();
    }

    public IPipelineBuilder<TRequestContext> ApplicationBuilder { get; }

    public IPipelineBuilder<TRequestContext> CreateApplicationBuilder() => ApplicationBuilder.New();

    public ICollection<EndpointDataSource<TRequestContext>> DataSources { get; }

    public IServiceProvider ServiceProvider => ApplicationBuilder.ApplicationServices;
}