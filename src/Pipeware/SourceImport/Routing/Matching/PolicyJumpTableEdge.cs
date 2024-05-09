// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/PolicyJumpTableEdge.cs
// Source Sha256: 19ba2cb4d422048c861059f2dd32ea7035291aae

// Originally licensed under:

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Routing.Matching;

/// <summary>
/// Represents an entry in a <see cref="PolicyJumpTable{TRequestContext}"/>.
/// </summary>
public readonly struct PolicyJumpTableEdge
{
    /// <summary>
    /// Constructs a new <see cref="PolicyJumpTableEdge"/> instance.
    /// </summary>
    /// <param name="state">Represents the match heuristic of the policy.</param>
    /// <param name="destination"></param>
    public PolicyJumpTableEdge(object state, int destination)
    {
        State = state ?? throw new System.ArgumentNullException(nameof(state));
        Destination = destination;
    }

    /// <summary>
    /// Gets the object used to represent the match heuristic. Can be a host, HTTP method, etc.
    /// depending on the matcher policy.
    /// </summary>
    public object State { get; }

    /// <summary>
    /// Gets the destination of the current entry.
    /// </summary>
    public int Destination { get; }
}