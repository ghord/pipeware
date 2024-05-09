// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/DfaState.cs
// Source Sha256: a33a47cbc824a2272b7924d7290ba37bfd3a7412

// Originally licensed under:

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Pipeware.Routing.Matching;

[DebuggerDisplay("{DebuggerToString(),nq}")]
internal readonly struct DfaState<TRequestContext> where TRequestContext : class, IRequestContext
{
    public readonly Candidate<TRequestContext>[] Candidates;
    public readonly IEndpointSelectorPolicy<TRequestContext>[] Policies;
    public readonly JumpTable PathTransitions;
    public readonly PolicyJumpTable<TRequestContext> PolicyTransitions;

    public DfaState(
        Candidate<TRequestContext>[] candidates,
        IEndpointSelectorPolicy<TRequestContext>[] policies,
        JumpTable pathTransitions,
        PolicyJumpTable<TRequestContext> policyTransitions)
    {
        Candidates = candidates;
        Policies = policies;
        PathTransitions = pathTransitions;
        PolicyTransitions = policyTransitions;
    }

    public string DebuggerToString()
    {
        return
            $"matches: {Candidates?.Length ?? 0}, " +
            $"path: ({PathTransitions?.DebuggerToString()}), " +
            $"policy: ({PolicyTransitions?.DebuggerToString()})";
    }
}