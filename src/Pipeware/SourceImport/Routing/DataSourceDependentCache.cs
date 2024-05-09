// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/DataSourceDependentCache.cs
// Source Sha256: 98713da723e2d7433bf81d7884e5e9e1a5cf8a7c

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using Pipeware;

namespace Pipeware.Routing;

// FYI: This class is also linked into MVC. If you make changes to the API you will
// also need to change MVC's usage.
internal sealed class DataSourceDependentCache<T, TRequestContext> : IDisposable where T : class
 where TRequestContext : class, IRequestContext
{
    private readonly EndpointDataSource<TRequestContext> _dataSource;
    private readonly Func<IReadOnlyList<Endpoint<TRequestContext>>, T> _initializeCore;
    private readonly Func<T> _initializer;
    private readonly Action<object?> _initializerWithState;

    private object _lock;
    private bool _initialized;
    private T? _value;

    private IDisposable? _disposable;
    private bool _disposed;

    public DataSourceDependentCache(EndpointDataSource<TRequestContext> dataSource, Func<IReadOnlyList<Endpoint<TRequestContext>>, T> initialize)
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        ArgumentNullException.ThrowIfNull(initialize);

        _dataSource = dataSource;
        _initializeCore = initialize;

        _initializer = Initialize;
        _initializerWithState = (state) => Initialize();
        _lock = new object();
    }

    // Note that we don't lock here, and think about that in the context of a 'push'. So when data gets 'pushed'
    // we start computing a new state, but we're still able to perform operations on the old state until we've
    // processed the update.
    [NotNullIfNotNull(nameof(_value))]
    public T? Value => _value;

    [MemberNotNull(nameof(_value))]
    public T EnsureInitialized()
    {
        return LazyInitializer.EnsureInitialized<T>(ref _value, ref _initialized, ref _lock, _initializer);
    }

    private T Initialize()
    {
        lock (_lock)
        {
            var changeToken = _dataSource.GetChangeToken();
            _value = _initializeCore(_dataSource.Endpoints);

            // Don't resubscribe if we're already disposed.
            if (_disposed)
            {
                return _value;
            }

            _disposable = changeToken.RegisterChangeCallback(_initializerWithState, null);
            return _value;
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (!_disposed)
            {
                _disposable?.Dispose();
                _disposable = null;

                // Tracking whether we're disposed or not prevents a race-condition
                // between disposal and Initialize(). If a change callback fires after
                // we dispose, then we don't want to reregister.
                _disposed = true;
            }
        }
    }
}