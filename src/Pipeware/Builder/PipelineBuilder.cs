// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: src/Http/Http/src/Builder/ApplicationBuilder.cs

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using Microsoft.Extensions.Options;
using Pipeware.Features;
using Pipeware.Internal;
using Pipeware.Routing;
using System.Diagnostics.CodeAnalysis;

namespace Pipeware.Builder;

public class PipelineBuilder<TRequestContext> : IPipelineBuilder<TRequestContext, PipelineBuilder<TRequestContext>>, IEndpointRouteBuilder<TRequestContext, PipelineBuilder<TRequestContext>> where TRequestContext : class, IRequestContext
{
    private const string PipelineFeaturesKey = "pipeline.Features";
    private const string ApplicationServicesKey = "application.Services";
    private const string DefaultDelegateKey = "pipeline.DefaultDelegate";
    private const string GlobalEndpointRouteBuilderKey = "__GlobalEndpointRouteBuilder";


    private List<EndpointDataSource<TRequestContext>> _dataSources = new();
    private List<Func<RequestDelegate<TRequestContext>, RequestDelegate<TRequestContext>>> _components = new();

    public PipelineBuilder(IServiceProvider serviceProvider, IFeatureCollection pipelineFeatures)

    {
        Properties = new Dictionary<string, object?>(StringComparer.Ordinal);

        SetProperty(GlobalEndpointRouteBuilderKey, this);
        SetProperty(ApplicationServicesKey, serviceProvider);
        SetProperty(PipelineFeaturesKey, pipelineFeatures);
        SetProperty<RequestDelegate<TRequestContext>>(DefaultDelegateKey, static context =>
        {
            // If we reach the end of the pipeline, but we have an endpoint, then something unexpected has happened.
            // This could happen if user code sets an endpoint, but they forgot to add the UseEndpoint middleware.
            var endpoint = context.GetEndpoint();
            var endpointRequestDelegate = endpoint?.RequestDelegate;
            if (endpointRequestDelegate != null)
            {
                var message =
                    $"The request reached the end of the pipeline without executing the endpoint: '{endpoint!.DisplayName}'. " +
                    $"Please register the EndpointMiddleware using '{nameof(IPipelineBuilder<TRequestContext>)}.UseEndpoints(...)' if using " +
                    $"routing.";
                throw new InvalidOperationException(message);
            }

            return Task.CompletedTask;
        });
    }

    public PipelineBuilder(IServiceProvider serviceProvider)
        : this(serviceProvider, new FeatureCollection())
    {

    }

    public IDictionary<string, object?> Properties { get; }

    private PipelineBuilder(PipelineBuilder<TRequestContext> builder)
    {
        Properties = new CopyOnWriteDictionary<string, object?>(builder.Properties, StringComparer.Ordinal);
    }

    public RequestDelegate<TRequestContext> DefaultDelegate
    {
        get => GetProperty<RequestDelegate<TRequestContext>>(DefaultDelegateKey);
        set => SetProperty(DefaultDelegateKey, value);
    }

    public IFeatureCollection PipelineFeatures => GetProperty<IFeatureCollection>(PipelineFeaturesKey);

    public IServiceProvider ApplicationServices
    {
        get => GetProperty<IServiceProvider>(ApplicationServicesKey);
        set => SetProperty(ApplicationServicesKey, value);
    }

    IServiceProvider IEndpointRouteBuilder<TRequestContext>.ServiceProvider => ApplicationServices;

    public ICollection<EndpointDataSource<TRequestContext>> DataSources => _dataSources;

    private void SetProperty<T>(string key, T value)
    {
        Properties[key] = value;
    }

    private T GetProperty<T>(string key)
    {
        return (T)Properties[key]!;
    }

    public IPipelineBuilder<TRequestContext> Use(Func<RequestDelegate<TRequestContext>, RequestDelegate<TRequestContext>> middleware)
    {
        _components.Add(middleware);

        return this;
    }

    public IPipelineBuilder<TRequestContext> New()
    {
        return new PipelineBuilder<TRequestContext>(this);
    }

    public RequestDelegate<TRequestContext> Build()
    {
        var app = DefaultDelegate;

        for (var c = _components.Count - 1; c >= 0; c--)
        {
            app = _components[c](app);
        }

        return app;
    }

    IPipelineBuilder<TRequestContext> IEndpointRouteBuilder<TRequestContext>.CreateApplicationBuilder() => New();
}
