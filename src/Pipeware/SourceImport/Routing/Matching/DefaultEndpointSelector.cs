// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/DefaultEndpointSelector.cs
// Source Sha256: becb14415dc832948af26c4a99eda9ec87f0e80b

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Pipeware;

namespace Pipeware.Routing.Matching;

internal sealed class DefaultEndpointSelector<TRequestContext> : EndpointSelector<TRequestContext> where TRequestContext : class, IRequestContext
{
    public override Task SelectAsync(
        TRequestContext requestContext,
        CandidateSet<TRequestContext> candidateSet)
    {
        ArgumentNullException.ThrowIfNull(requestContext);
        ArgumentNullException.ThrowIfNull(candidateSet);

        Select(requestContext, candidateSet.Candidates);
        return Task.CompletedTask;
    }

    internal static void Select(TRequestContext requestContext, Span<CandidateState<TRequestContext>> candidateState)
    {
        // Fast path: We can specialize for trivial numbers of candidates since there can
        // be no ambiguities
        switch (candidateState.Length)
        {
            case 0:
                {
                    // Do nothing
                    break;
                }

            case 1:
                {
                    ref var state = ref candidateState[0];
                    if (CandidateSet<TRequestContext>.IsValidCandidate(ref state))
                    {
                        requestContext.SetEndpoint(state.Endpoint);
                        requestContext.GetRouteValuesFeature().RouteValues = state.Values!;
                    }

                    break;
                }

            default:
                {
                    // Slow path: There's more than one candidate (to say nothing of validity) so we
                    // have to process for ambiguities.
                    ProcessFinalCandidates(requestContext, candidateState);
                    break;
                }
        }
    }

    private static void ProcessFinalCandidates(
        TRequestContext requestContext,
        Span<CandidateState<TRequestContext>> candidateState)
    {
        Endpoint<TRequestContext>? endpoint = null;
        RouteValueDictionary? values = null;
        int? foundScore = null;
        for (var i = 0; i < candidateState.Length; i++)
        {
            ref var state = ref candidateState[i];
            if (!CandidateSet<TRequestContext>.IsValidCandidate(ref state))
            {
                continue;
            }

            if (foundScore == null)
            {
                // This is the first match we've seen - speculatively assign it.
                endpoint = state.Endpoint;
                values = state.Values;
                foundScore = state.Score;
            }
            else if (foundScore < state.Score)
            {
                // This candidate is lower priority than the one we've seen
                // so far, we can stop.
                //
                // Don't worry about the 'null < state.Score' case, it returns false.
                break;
            }
            else if (foundScore == state.Score)
            {
                // This is the second match we've found of the same score, so there
                // must be an ambiguity.
                //
                // Don't worry about the 'null == state.Score' case, it returns false.

                ReportAmbiguity(candidateState);

                // Unreachable, ReportAmbiguity always throws.
                throw new NotSupportedException();
            }
        }

        if (endpoint != null)
        {
            requestContext.SetEndpoint(endpoint);
            requestContext.GetRouteValuesFeature().RouteValues = values!;
        }
    }

    private static void ReportAmbiguity(Span<CandidateState<TRequestContext>> candidateState)
    {
        // If we get here it's the result of an ambiguity - we're OK with this
        // being a littler slower and more allocatey.
        var matches = new List<Endpoint<TRequestContext>>();
        for (var i = 0; i < candidateState.Length; i++)
        {
            ref var state = ref candidateState[i];
            if (CandidateSet<TRequestContext>.IsValidCandidate(ref state))
            {
                matches.Add(state.Endpoint);
            }
        }

        var message = string.Format("The request matched multiple endpoints. Matches: {0}{0}{1}", Environment.NewLine, string.Join(Environment.NewLine, matches.Select(e => e.DisplayName)));
        throw new AmbiguousMatchException(message);
    }
}