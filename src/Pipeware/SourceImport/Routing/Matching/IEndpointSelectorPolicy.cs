// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/IEndpointSelectorPolicy.cs
// Source Sha256: 775a042f4d024d7573fe403717995f0c84cf73e2

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;

namespace Pipeware.Routing.Matching;

/// <summary>
/// A <see cref="MatcherPolicy{TRequestContext}"/> interface that can implemented to filter endpoints
/// in a <see cref="CandidateSet{TRequestContext}"/>. Implementations of <see cref="IEndpointSelectorPolicy{TRequestContext}"/> must
/// inherit from <see cref="MatcherPolicy{TRequestContext}"/> and should be registered in
/// the dependency injection container as singleton services of type <see cref="MatcherPolicy{TRequestContext}"/>.
/// </summary>
public interface IEndpointSelectorPolicy<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Returns a value that indicates whether the <see cref="IEndpointSelectorPolicy{TRequestContext}"/> applies
    /// to any endpoint in <paramref name="endpoints"/>.
    /// </summary>
    /// <param name="endpoints">The set of candidate <see cref="Endpoint{TRequestContext}"/> values.</param>
    /// <returns>
    /// <c>true</c> if the policy applies to any endpoint in <paramref name="endpoints"/>, otherwise <c>false</c>.
    /// </returns>
    bool AppliesToEndpoints(IReadOnlyList<Endpoint<TRequestContext>> endpoints);

    /// <summary>
    /// Applies the policy to the <see cref="CandidateSet{TRequestContext}"/>.
    /// </summary>
    /// <param name="httpContext">
    /// The <see cref="TRequestContext"/> associated with the current request.
    /// </param>
    /// <param name="candidates">The <see cref="CandidateSet{TRequestContext}"/>.</param>
    /// <remarks>
    /// <para>
    /// Implementations of <see cref="IEndpointSelectorPolicy{TRequestContext}"/> should implement this method
    /// and filter the set of candidates in the <paramref name="candidates"/> by setting
    /// <see cref="CandidateSet.SetValidity(int, bool)"/> to <c>false</c> where desired.
    /// </para>
    /// <para>
    /// To signal an error condition, the <see cref="IEndpointSelectorPolicy{TRequestContext}"/> should assign the endpoint by
    /// calling <see cref="EndpointHttpContextExtensions.SetEndpoint(HttpContext, Endpoint)"/>
    /// and setting <see cref="HttpRequest.RouteValues"/> to an
    /// <see cref="Endpoint{TRequestContext}"/> value that will produce the desired error when executed.
    /// </para>
    /// </remarks>
    Task ApplyAsync(TRequestContext requestContext, CandidateSet<TRequestContext> candidates);
}