// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/CandidateState.cs
// Source Sha256: 8eae3a22c39cc578050cff09b8a217931399faf6

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;

namespace Pipeware.Routing.Matching;

/// <summary>
/// The state associated with a candidate in a <see cref="CandidateSet{TRequestContext}"/>.
/// </summary>
public struct CandidateState<TRequestContext> where TRequestContext : class, IRequestContext
{
    internal CandidateState(Endpoint<TRequestContext> endpoint, int score)
    {
        Endpoint = endpoint;
        Score = score;
        Values = null;
    }

    internal CandidateState(Endpoint<TRequestContext> endpoint, RouteValueDictionary? values, int score)
    {
        Endpoint = endpoint;
        Values = values;
        Score = score;
    }

    /// <summary>
    /// Gets the <see cref="Http.Endpoint{TRequestContext}"/>.
    /// </summary>
    public Endpoint<TRequestContext> Endpoint { get; }

    /// <summary>
    /// Gets the score of the <see cref="Http.Endpoint{TRequestContext}"/> within the current
    /// <see cref="CandidateSet{TRequestContext}"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Candidates within a set are ordered in priority order and then assigned a
    /// sequential score value based on that ordering. Candiates with the same
    /// score are considered to have equal priority.
    /// </para>
    /// <para>
    /// The score values are used in the <see cref="EndpointSelector{TRequestContext}"/> to determine
    /// whether a set of matching candidates is an ambiguous match.
    /// </para>
    /// </remarks>
    public int Score { get; }

    /// <summary>
    /// Gets <see cref="RouteValueDictionary"/> associated with the
    /// <see cref="Http.Endpoint{TRequestContext}"/> and the current request.
    /// </summary>
    public RouteValueDictionary? Values { get; internal set; }
}