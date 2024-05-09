// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/DataSourceDependentMatcher.cs
// Source Sha256: c50af2faca30e60d2fe80153437eaceff277be13

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Pipeware;

namespace Pipeware.Routing.Matching;

internal sealed class DataSourceDependentMatcher<TRequestContext> : Matcher<TRequestContext> where TRequestContext : class, IRequestContext
{
    private readonly Func<MatcherBuilder<TRequestContext>> _matcherBuilderFactory;
    private readonly DataSourceDependentCache<Matcher<TRequestContext>, TRequestContext> _cache;

    public DataSourceDependentMatcher(
        EndpointDataSource<TRequestContext> dataSource,
        Lifetime lifetime,
        Func<MatcherBuilder<TRequestContext>> matcherBuilderFactory)
    {
        _matcherBuilderFactory = matcherBuilderFactory;

        _cache = new DataSourceDependentCache<Matcher<TRequestContext>, TRequestContext>(dataSource, CreateMatcher);
        _cache.EnsureInitialized();

        // This will Dispose the cache when the lifetime is disposed, this allows
        // the service provider to manage the lifetime of the cache.
        lifetime.Cache = _cache;
    }

    // Used in tests
    internal Matcher<TRequestContext> CurrentMatcher => _cache.Value!;

    public override Task MatchAsync(TRequestContext requestContext)
    {
        return CurrentMatcher.MatchAsync(requestContext);
    }

    private Matcher<TRequestContext> CreateMatcher(IReadOnlyList<Endpoint<TRequestContext>> endpoints)
    {
        var builder = _matcherBuilderFactory();
        var seenEndpointNames = new Dictionary<string, string?>();
        for (var i = 0; i < endpoints.Count; i++)
        {
            // By design we only look at RouteEndpoint here. It's possible to
            // register other endpoint types, which are non-routable, and it's
            // ok that we won't route to them.
            if (endpoints[i] is RouteEndpoint<TRequestContext> endpoint)
            {
                // Validate that endpoint names are unique.
                var endpointName = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName;
                if (endpointName is not null)
                {
                    if (seenEndpointNames.TryGetValue(endpointName, out var existingEndpoint))
                    {
                        throw new InvalidOperationException($"Duplicate endpoint name '{endpointName}' found on '{endpoint.DisplayName}' and '{existingEndpoint}'. Endpoint names must be globally unique.");
                    }

                    seenEndpointNames.Add(endpointName, endpoint.DisplayName ?? endpoint.RoutePattern.RawText);
                }

                // We check for duplicate endpoint names on all endpoints regardless
                // of whether they suppress matching because endpoint names can be
                // used in OpenAPI specifications as well.
                if (endpoint.Metadata.GetMetadata<ISuppressMatchingMetadata>()?.SuppressMatching != true)
                {
                    builder.AddEndpoint(endpoint);
                }
            }
        }

        return builder.Build();
    }

    // Used to tie the lifetime of a DataSourceDependentCache to the service provider
    public sealed class Lifetime : IDisposable
    {
        private readonly object _lock = new object();
        private DataSourceDependentCache<Matcher<TRequestContext>, TRequestContext>? _cache;
        private bool _disposed;

        public DataSourceDependentCache<Matcher<TRequestContext>, TRequestContext>? Cache
        {
            get => _cache;
            set
            {
                lock (_lock)
                {
                    if (_disposed)
                    {
                        value?.Dispose();
                    }

                    _cache = value;
                }
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _cache?.Dispose();
                _cache = null;

                _disposed = true;
            }
        }
    }
}