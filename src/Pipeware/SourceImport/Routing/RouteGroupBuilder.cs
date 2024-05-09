// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/RouteGroupBuilder.cs
// Source Sha256: 513ff3651b7a33f288b2b4f43f661a4489575904

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware.Builder;
using Pipeware;
using Pipeware.Routing.Patterns;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Pipeware.Routing;

/// <summary>
/// A builder for defining groups of endpoints with a common prefix that implements both the <see cref="IEndpointRouteBuilder{TRequestContext}"/>
/// and <see cref="IEndpointConventionBuilder{TRequestContext}"/> interfaces. This can be used to add endpoints with the prefix defined by
/// <see cref="EndpointRouteBuilderExtensions.MapGroup(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder, RoutePattern)"/>
/// and to customize those endpoints using conventions.
/// </summary>
public sealed class RouteGroupBuilder<TRequestContext> : IEndpointRouteBuilder<TRequestContext>, IEndpointConventionBuilder<TRequestContext>
, IEndpointConventionBuilder<TRequestContext, RouteGroupBuilder<TRequestContext>>
, IEndpointRouteBuilder<TRequestContext, RouteGroupBuilder<TRequestContext>> where TRequestContext : class, IRequestContext
{
    private readonly IEndpointRouteBuilder<TRequestContext> _outerEndpointRouteBuilder;
    private readonly RoutePattern _partialPrefix;

    private readonly List<EndpointDataSource<TRequestContext>> _dataSources = new();
    private readonly List<Action<EndpointBuilder<TRequestContext>>> _conventions = new();
    private readonly List<Action<EndpointBuilder<TRequestContext>>> _finallyConventions = new();

    internal RouteGroupBuilder(IEndpointRouteBuilder<TRequestContext> outerEndpointRouteBuilder, RoutePattern partialPrefix)
    {
        _outerEndpointRouteBuilder = outerEndpointRouteBuilder;
        _partialPrefix = partialPrefix;
        _outerEndpointRouteBuilder.DataSources.Add(new GroupEndpointDataSource(this));
    }

    IServiceProvider IEndpointRouteBuilder<TRequestContext>.ServiceProvider => _outerEndpointRouteBuilder.ServiceProvider;
    IPipelineBuilder<TRequestContext> IEndpointRouteBuilder<TRequestContext>.CreateApplicationBuilder() => _outerEndpointRouteBuilder.CreateApplicationBuilder();
    ICollection<EndpointDataSource<TRequestContext>> IEndpointRouteBuilder<TRequestContext>.DataSources => _dataSources;
    void IEndpointConventionBuilder<TRequestContext>.Add(Action<EndpointBuilder<TRequestContext>> convention) => _conventions.Add(convention);
    void IEndpointConventionBuilder<TRequestContext>.Finally(Action<EndpointBuilder<TRequestContext>> finalConvention) => _finallyConventions.Add(finalConvention);

    private sealed class GroupEndpointDataSource : EndpointDataSource<TRequestContext>, IDisposable
    {
        private readonly RouteGroupBuilder<TRequestContext> _routeGroupBuilder;
        private CompositeEndpointDataSource<TRequestContext>? _compositeDataSource;

        public GroupEndpointDataSource(RouteGroupBuilder<TRequestContext> groupRouteBuilder)
        {
            _routeGroupBuilder = groupRouteBuilder;
        }

        public override IReadOnlyList<Endpoint<TRequestContext>> Endpoints =>
            GetGroupedEndpointsWithNullablePrefix(null, Array.Empty<Action<EndpointBuilder<TRequestContext>>>(),
                Array.Empty<Action<EndpointBuilder<TRequestContext>>>(), _routeGroupBuilder._outerEndpointRouteBuilder.ServiceProvider);

        public override IReadOnlyList<Endpoint<TRequestContext>> GetGroupedEndpoints(RouteGroupContext<TRequestContext> context) =>
            GetGroupedEndpointsWithNullablePrefix(context.Prefix, context.Conventions, context.FinallyConventions, context.ApplicationServices);

        public IReadOnlyList<Endpoint<TRequestContext>> GetGroupedEndpointsWithNullablePrefix(
            RoutePattern? prefix,
            IReadOnlyList<Action<EndpointBuilder<TRequestContext>>> conventions,
            IReadOnlyList<Action<EndpointBuilder<TRequestContext>>> finallyConventions,
            IServiceProvider applicationServices)
        {
            return _routeGroupBuilder._dataSources.Count switch
            {
                0 => Array.Empty<Endpoint<TRequestContext>>(),
                1 => _routeGroupBuilder._dataSources[0].GetGroupedEndpoints(GetNextRouteGroupContext(prefix, conventions, finallyConventions, applicationServices)),
                _ => SelectEndpointsFromAllDataSources(GetNextRouteGroupContext(prefix, conventions, finallyConventions, applicationServices)),
            };
        }

        public override IChangeToken GetChangeToken() => _routeGroupBuilder._dataSources.Count switch
        {
            0 => NullChangeToken.Singleton,
            1 => _routeGroupBuilder._dataSources[0].GetChangeToken(),
            _ => GetCompositeChangeToken(),
        };

        public void Dispose()
        {
            _compositeDataSource?.Dispose();

            foreach (var dataSource in _routeGroupBuilder._dataSources)
            {
                (dataSource as IDisposable)?.Dispose();
            }
        }

        private RouteGroupContext<TRequestContext> GetNextRouteGroupContext(
            RoutePattern? prefix,
            IReadOnlyList<Action<EndpointBuilder<TRequestContext>>> conventions,
            IReadOnlyList<Action<EndpointBuilder<TRequestContext>>> finallyConventions,
            IServiceProvider applicationServices)
        {
            var fullPrefix = RoutePatternFactory.Combine(prefix, _routeGroupBuilder._partialPrefix);
            // Apply conventions passed in from the outer group first so their metadata is added earlier in the list at a lower precedent.
            var combinedConventions = RoutePatternFactory.CombineLists(conventions, _routeGroupBuilder._conventions);
            var combinedFinallyConventions = RoutePatternFactory.CombineLists(_routeGroupBuilder._finallyConventions, finallyConventions);
            return new RouteGroupContext<TRequestContext>
            {
                Prefix = fullPrefix,
                Conventions = combinedConventions,
                FinallyConventions = combinedFinallyConventions,
                ApplicationServices = applicationServices
            };
        }

        private IReadOnlyList<Endpoint<TRequestContext>> SelectEndpointsFromAllDataSources(RouteGroupContext<TRequestContext> context)
        {
            var groupedEndpoints = new List<Endpoint<TRequestContext>>();

            foreach (var dataSource in _routeGroupBuilder._dataSources)
            {
                groupedEndpoints.AddRange(dataSource.GetGroupedEndpoints(context));
            }

            return groupedEndpoints;
        }

        private IChangeToken GetCompositeChangeToken()
        {
            // We are not guarding against concurrent RouteGroupBuilder._dataSources mutation.
            // This is only to avoid double initialization of _compositeDataSource if GetChangeToken() is called concurrently.
            lock (_routeGroupBuilder._dataSources)
            {
                _compositeDataSource ??= new CompositeEndpointDataSource<TRequestContext>(_routeGroupBuilder._dataSources);
            }

            return _compositeDataSource.GetChangeToken();
        }
    }
}