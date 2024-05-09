// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/EndpointMetadataComparer.cs
// Source Sha256: 4ea5d01d2609664d5338ed57f784f929493149a9

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Linq;
using Pipeware;
using Microsoft.Extensions.DependencyInjection;

namespace Pipeware.Routing.Matching;

/// <summary>
/// A comparer that can order <see cref="Endpoint{TRequestContext}"/> instances based on implementations of
/// <see cref="IEndpointComparerPolicy{TRequestContext}" />. The implementation can be retrieved from the service
/// provider and provided to <see cref="CandidateSet.ExpandEndpoint(int, IReadOnlyList{Endpoint<TRequestContext>}, IComparer{Endpoint<TRequestContext>})"/>.
/// </summary>
public sealed class EndpointMetadataComparer<TRequestContext> : IComparer<Endpoint<TRequestContext>> where TRequestContext : class, IRequestContext
{
    private readonly IServiceProvider _services;
    private IComparer<Endpoint<TRequestContext>>[]? _comparers;

    // This type is **INTENDED** for use in MatcherPolicy instances yet is also needs the MatcherPolicy instances.
    // using IServiceProvider to break the cycle.
    internal EndpointMetadataComparer(IServiceProvider services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _services = services;
    }

    private IComparer<Endpoint<TRequestContext>>[] Comparers
    {
        get
        {
            if (_comparers == null)
            {
                _comparers = _services.GetServices<MatcherPolicy<TRequestContext>>()
                    .OrderBy(p => p.Order)
                    .OfType<IEndpointComparerPolicy<TRequestContext>>()
                    .Select(p => p.Comparer)
                    .ToArray();
            }

            return _comparers;
        }
    }

    int IComparer<Endpoint<TRequestContext>>.Compare(Endpoint<TRequestContext>? x, Endpoint<TRequestContext>? y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        var comparers = Comparers;
        for (var i = 0; i < comparers.Length; i++)
        {
            var compare = comparers[i].Compare(x, y);
            if (compare != 0)
            {
                return compare;
            }
        }

        return 0;
    }
}

/// <summary>
/// A base class for <see cref="IComparer{Endpoint<TRequestContext>}"/> implementations that use
/// a specific type of metadata from <see cref="Endpoint.Metadata"/> for comparison.
/// Useful for implementing <see cref="IEndpointComparerPolicy.Comparer"/>.
/// </summary>
/// <typeparam name="TMetadata">
/// The type of metadata to compare. Typically this is a type of metadata related
/// to the application concern being handled.
/// </typeparam>
public abstract class EndpointMetadataComparer<TMetadata, TRequestContext> : IComparer<Endpoint<TRequestContext>> where TMetadata : class
 where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// A default instance of the <see cref="EndpointMetadataComparer{TRequestContext}"/>.
    /// </summary>
    public static readonly EndpointMetadataComparer<TMetadata, TRequestContext> Default = new DefaultComparer<TMetadata>();

    /// <summary>
    /// Compares two objects and returns a value indicating whether one is less than, equal to,
    /// or greater than the other.
    /// </summary>
    /// <param name="x">The first object to compare.</param>
    /// <param name="y">The second object to compare.</param>
    /// <returns>
    /// An implementation of this method must return a value less than zero if
    /// x is less than y, zero if x is equal to y, or a value greater than zero if x is
    /// greater than y.
    /// </returns>
    public int Compare(Endpoint<TRequestContext>? x, Endpoint<TRequestContext>? y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        return CompareMetadata(GetMetadata(x), GetMetadata(y));
    }

    /// <summary>
    /// Gets the metadata of type <typeparamref name="TMetadata"/> from the provided endpoint.
    /// </summary>
    /// <param name="endpoint">The <see cref="Endpoint{TRequestContext}"/>.</param>
    /// <returns>The <typeparamref name="TMetadata"/> instance or <c>null</c>.</returns>
    protected virtual TMetadata? GetMetadata(Endpoint<TRequestContext> endpoint)
    {
        return endpoint.Metadata.GetMetadata<TMetadata>();
    }

    /// <summary>
    /// Compares two <typeparamref name="TMetadata"/> instances.
    /// </summary>
    /// <param name="x">The first object to compare.</param>
    /// <param name="y">The second object to compare.</param>
    /// <returns>
    /// An implementation of this method must return a value less than zero if
    /// x is less than y, zero if x is equal to y, or a value greater than zero if x is
    /// greater than y.
    /// </returns>
    /// <remarks>
    /// The base-class implementation of this method will compare metadata based on whether
    /// or not they are <c>null</c>. The effect of this is that when endpoints are being
    /// compared, the endpoint that defines an instance of <typeparamref name="TMetadata"/>
    /// will be considered higher priority.
    /// </remarks>
    protected virtual int CompareMetadata(TMetadata? x, TMetadata? y)
    {
        // The default policy is that if x endpoint defines TMetadata, and
        // y endpoint does not, then x is *more specific* than y. We return
        // -1 for this case so that x will come first in the sort order.

        if (x == null && y != null)
        {
            // y is more specific
            return 1;
        }
        else if (x != null && y == null)
        {
            // x is more specific
            return -1;
        }

        // both endpoints have this metadata, or both do not have it, they have
        // the same specificity.
        return 0;
    }

    private sealed class DefaultComparer<T> : EndpointMetadataComparer<T, TRequestContext>where T : class
    {
    }
}