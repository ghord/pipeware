// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/INodeBuilderPolicy.cs
// Source Sha256: 9ac7217c9815ff9b9f981fee713d97646b29cd16

// Originally licensed under:

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;

namespace Pipeware.Routing.Matching;

/// <summary>
/// Implements an interface for a matcher policy with support for generating graph representations of the endpoints.
/// </summary>
public interface INodeBuilderPolicy<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Evaluates if the policy matches any of the endpoints provided in <paramref name="endpoints"/>.
    /// </summary>
    /// <param name="endpoints">A list of <see cref="Endpoint{TRequestContext}"/>.</param>
    /// <returns><see langword="true"/> if the policy applies to any of the provided <paramref name="endpoints"/>.</returns>
    bool AppliesToEndpoints(IReadOnlyList<Endpoint<TRequestContext>> endpoints);

    /// <summary>
    /// Generates a graph that representations the relationship between endpoints and hosts.
    /// </summary>
    /// <param name="endpoints">A list of <see cref="Endpoint{TRequestContext}"/>.</param>
    /// <returns>A graph representing the relationship between endpoints and hosts.</returns>
    IReadOnlyList<PolicyNodeEdge<TRequestContext>> GetEdges(IReadOnlyList<Endpoint<TRequestContext>> endpoints);

    /// <summary>
    /// Constructs a jump table given the a set of <paramref name="edges"/>.
    /// </summary>
    /// <param name="exitDestination">The default destination for lookups.</param>
    /// <param name="edges">A list of <see cref="PolicyJumpTableEdge"/>.</param>
    /// <returns>A <see cref="PolicyJumpTable{TRequestContext}"/> instance.</returns>
    PolicyJumpTable<TRequestContext> BuildJumpTable(int exitDestination, IReadOnlyList<PolicyJumpTableEdge> edges);
}