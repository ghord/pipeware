// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/PolicyNodeEdge.cs
// Source Sha256: 842c604b55be6a489c85890ac98bd3f47c840532

// Originally licensed under:

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;

namespace Pipeware.Routing.Matching;

/// <summary>
/// Represents an edge in a matcher policy graph.
/// </summary>
public readonly struct PolicyNodeEdge<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Constructs a new <see cref="PolicyNodeEdge{TRequestContext}"/> instance.
    /// </summary>
    /// <param name="state">Represents the match heuristic of the policy.</param>
    /// <param name="endpoints">Represents the endpoints that match the policy</param>
    public PolicyNodeEdge(object state, IReadOnlyList<Endpoint<TRequestContext>> endpoints)
    {
        State = state ?? throw new System.ArgumentNullException(nameof(state));
        Endpoints = endpoints ?? throw new System.ArgumentNullException(nameof(endpoints));
    }

    /// <summary>
    /// Gets the endpoints that match the policy defined by <see cref="State"/>.
    /// </summary>
    public IReadOnlyList<Endpoint<TRequestContext>> Endpoints { get; }

    /// <summary>
    /// Gets the object used to represent the match heuristic. Can be a host, HTTP method, etc.
    /// depending on the matcher policy.
    /// </summary>
    public object State { get; }
}