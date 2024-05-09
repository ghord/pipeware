// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/MatcherPolicy.cs
// Source Sha256: ea9bd79818e3aef111b4dec8efb9ed53fae1e891

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;
using Pipeware.Routing.Matching;

namespace Pipeware.Routing;

/// <summary>
/// Defines a policy that applies behaviors to the URL matcher. Implementations
/// of <see cref="MatcherPolicy{TRequestContext}"/> and related interfaces must be registered
/// in the dependency injection container as singleton services of type
/// <see cref="MatcherPolicy{TRequestContext}"/>.
/// </summary>
/// <remarks>
/// <see cref="MatcherPolicy{TRequestContext}"/> implementations can implement the following
/// interfaces <see cref="IEndpointComparerPolicy{TRequestContext}"/>, <see cref="IEndpointSelectorPolicy{TRequestContext}"/>,
/// and <see cref="INodeBuilderPolicy{TRequestContext}"/>.
/// </remarks>
public abstract class MatcherPolicy<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Gets a value that determines the order the <see cref="MatcherPolicy{TRequestContext}"/> should
    /// be applied. Policies are applied in ascending numeric value of the <see cref="Order"/>
    /// property.
    /// </summary>
    public abstract int Order { get; }

    /// <summary>
    /// Returns a value that indicates whether the provided <paramref name="endpoints"/> contains
    /// one or more dynamic endpoints.
    /// </summary>
    /// <param name="endpoints">The set of endpoints.</param>
    /// <returns><c>true</c> if a dynamic endpoint is found; otherwise returns <c>false</c>.</returns>
    /// <remarks>
    /// <para>
    /// The presence of <see cref="IDynamicEndpointMetadata"/> signifies that an endpoint that may be replaced
    /// during processing by an <see cref="IEndpointSelectorPolicy{TRequestContext}"/>.
    /// </para>
    /// <para>
    /// An implementation of <see cref="INodeBuilderPolicy{TRequestContext}"/> should also implement <see cref="IEndpointSelectorPolicy{TRequestContext}"/>
    /// and use its <see cref="IEndpointSelectorPolicy{TRequestContext}"/> implementation when a node contains a dynamic endpoint.
    /// <see cref="INodeBuilderPolicy{TRequestContext}"/> implementations rely on caching of data based on a static set of endpoints. This
    /// is not possible when endpoints are replaced dynamically.
    /// </para>
    /// </remarks>
    protected static bool ContainsDynamicEndpoints(IReadOnlyList<Endpoint<TRequestContext>> endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        for (var i = 0; i < endpoints.Count; i++)
        {
            var metadata = endpoints[i].Metadata.GetMetadata<IDynamicEndpointMetadata>();
            if (metadata?.IsDynamic == true)
            {
                return true;
            }
        }

        return false;
    }
}