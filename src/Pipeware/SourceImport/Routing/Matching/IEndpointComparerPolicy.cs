// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/IEndpointComparerPolicy.cs
// Source Sha256: 58aff7e4945b52dc01d55819efb3fbc48bcb5da6

// Originally licensed under:

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;

namespace Pipeware.Routing.Matching;

/// <summary>
/// A <see cref="MatcherPolicy{TRequestContext}"/> interface that can be implemented to sort
/// endpoints. Implementations of <see cref="IEndpointComparerPolicy{TRequestContext}"/> must
/// inherit from <see cref="MatcherPolicy{TRequestContext}"/> and should be registered in
/// the dependency injection container as singleton services of type <see cref="MatcherPolicy{TRequestContext}"/>.
/// </summary>
/// <remarks>
/// <para>
/// Candidates in a <see cref="CandidateSet{TRequestContext}"/> are sorted based on their priority. Defining
/// a <see cref="IEndpointComparerPolicy{TRequestContext}"/> adds an additional criterion to the sorting
/// operation used to order candidates.
/// </para>
/// <para>
/// As an example, the implementation of <see cref="HttpMethodMatcherPolicy"/> implements
/// <see cref="IEndpointComparerPolicy{TRequestContext}"/> to ensure that endpoints matching specific HTTP
/// methods are sorted with a higher priority than endpoints without a specific HTTP method
/// requirement.
/// </para>
/// </remarks>
public interface IEndpointComparerPolicy<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Gets an <see cref="IComparer{Endpoint<TRequestContext>}"/> that will be used to sort the endpoints.
    /// </summary>
    IComparer<Endpoint<TRequestContext>> Comparer { get; }
}