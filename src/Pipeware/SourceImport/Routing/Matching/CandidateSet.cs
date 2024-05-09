// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/CandidateSet.cs
// Source Sha256: 77b84b671afe4356905cfd9d997cebc3f5f2259f

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Pipeware;

namespace Pipeware.Routing.Matching;

/// <summary>
/// Represents a set of <see cref="Endpoint{TRequestContext}"/> candidates that have been matched
/// by the routing system. Used by implementations of <see cref="EndpointSelector{TRequestContext}"/>
/// and <see cref="IEndpointSelectorPolicy{TRequestContext}"/>.
/// </summary>
public sealed class CandidateSet<TRequestContext> where TRequestContext : class, IRequestContext
{
    internal CandidateState<TRequestContext>[] Candidates;

    /// <summary>
    /// <para>
    /// Initializes a new instances of the <see cref="CandidateSet{TRequestContext}"/> class with the provided <paramref name="endpoints"/>,
    /// <paramref name="values"/>, and <paramref name="scores"/>.
    /// </para>
    /// <para>
    /// The constructor is provided to enable unit tests of implementations of <see cref="EndpointSelector{TRequestContext}"/>
    /// and <see cref="IEndpointSelectorPolicy{TRequestContext}"/>.
    /// </para>
    /// </summary>
    /// <param name="endpoints">The list of endpoints, sorted in descending priority order.</param>
    /// <param name="values">The list of <see cref="RouteValueDictionary"/> instances.</param>
    /// <param name="scores">The list of endpoint scores. <see cref="CandidateState.Score"/>.</param>
    public CandidateSet(Endpoint<TRequestContext>[] endpoints, RouteValueDictionary[] values, int[] scores)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(scores);

        if (endpoints.Length != values.Length || endpoints.Length != scores.Length)
        {
            throw new ArgumentException($"The provided {nameof(endpoints)}, {nameof(values)}, and {nameof(scores)} must have the same length.");
        }

        Candidates = new CandidateState<TRequestContext>[endpoints.Length];
        for (var i = 0; i < endpoints.Length; i++)
        {
            Candidates[i] = new CandidateState<TRequestContext>(endpoints[i], values[i], scores[i]);
        }
    }

    // Used in tests.
    internal CandidateSet(Candidate<TRequestContext>[] candidates)
    {
        Candidates = new CandidateState<TRequestContext>[candidates.Length];
        for (var i = 0; i < candidates.Length; i++)
        {
            Candidates[i] = new CandidateState<TRequestContext>(candidates[i].Endpoint, candidates[i].Score);
        }
    }

    internal CandidateSet(CandidateState<TRequestContext>[] candidates)
    {
        Candidates = candidates;
    }

    /// <summary>
    /// Gets the count of candidates in the set.
    /// </summary>
    public int Count => Candidates.Length;

    /// <summary>
    /// Gets the <see cref="CandidateState{TRequestContext}"/> associated with the candidate <see cref="Endpoint{TRequestContext}"/>
    /// at <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The candidate index.</param>
    /// <returns>
    /// A reference to the <see cref="CandidateState{TRequestContext}"/>. The result is returned by reference.
    /// </returns>
    public ref CandidateState<TRequestContext> this[int index]
    {
        // Note that this is a ref-return because of performance.
        // We don't want to copy these fat structs if it can be avoided.

        // PERF: Force inlining
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            // Friendliness for inlining
            if ((uint)index >= Count)
            {
                ThrowIndexArgumentOutOfRangeException();
            }

            return ref Candidates[index];
        }
    }

    /// <summary>
    /// Gets a value which indicates where the <see cref="Http.Endpoint{TRequestContext}"/> is considered
    /// a valid candidate for the current request.
    /// </summary>
    /// <param name="index">The candidate index.</param>
    /// <returns>
    /// <c>true</c> if the candidate at position <paramref name="index"/> is considered valid
    /// for the current request, otherwise <c>false</c>.
    /// </returns>
    public bool IsValidCandidate(int index)
    {
        // Friendliness for inlining
        if ((uint)index >= Count)
        {
            ThrowIndexArgumentOutOfRangeException();
        }

        return IsValidCandidate(ref Candidates[index]);
    }

    internal static bool IsValidCandidate(ref CandidateState<TRequestContext> candidate)
    {
        return candidate.Score >= 0;
    }

    /// <summary>
    /// Sets the validity of the candidate at the provided index.
    /// </summary>
    /// <param name="index">The candidate index.</param>
    /// <param name="value">
    /// The value to set. If <c>true</c> the candidate is considered valid for the current request.
    /// </param>
    public void SetValidity(int index, bool value)
    {
        // Friendliness for inlining
        if ((uint)index >= Count)
        {
            ThrowIndexArgumentOutOfRangeException();
        }

        ref var original = ref Candidates[index];
        SetValidity(ref original, value);
    }

    internal static void SetValidity(ref CandidateState<TRequestContext> candidate, bool value)
    {
        var originalScore = candidate.Score;
        var score = originalScore >= 0 ^ value ? ~originalScore : originalScore;
        candidate = new CandidateState<TRequestContext>(candidate.Endpoint, candidate.Values, score);
    }

    /// <summary>
    /// Replaces the <see cref="Endpoint{TRequestContext}"/> at the provided <paramref name="index"/> with the
    /// provided <paramref name="endpoint"/>.
    /// </summary>
    /// <param name="index">The candidate index.</param>
    /// <param name="endpoint">
    /// The <see cref="Endpoint{TRequestContext}"/> to replace the original <see cref="Endpoint{TRequestContext}"/> at
    /// the <paramref name="index"/>. If <paramref name="endpoint"/> is <c>null</c>. the candidate will be marked
    /// as invalid.
    /// </param>
    /// <param name="values">
    /// The <see cref="RouteValueDictionary"/> to replace the original <see cref="RouteValueDictionary"/> at
    /// the <paramref name="index"/>.
    /// </param>
    public void ReplaceEndpoint(int index, Endpoint<TRequestContext>? endpoint, RouteValueDictionary? values)
    {
        // Friendliness for inlining
        if ((uint)index >= Count)
        {
            ThrowIndexArgumentOutOfRangeException();
        }

        // CandidateState allows a null-valued endpoint. However a validate candidate should never have a null endpoint
        // We'll make lives easier for matcher policies by declaring it as non-null.
        Candidates[index] = new CandidateState<TRequestContext>(endpoint!, values, Candidates[index].Score);

        if (endpoint == null)
        {
            SetValidity(index, false);
        }
    }

    /// <summary>
    /// Replaces the <see cref="Endpoint{TRequestContext}"/> at the provided <paramref name="index"/> with the
    /// provided <paramref name="endpoints"/>.
    /// </summary>
    /// <param name="index">The candidate index.</param>
    /// <param name="endpoints">
    /// The list of endpoints <see cref="Endpoint{TRequestContext}"/> to replace the original <see cref="Endpoint{TRequestContext}"/> at
    /// the <paramref name="index"/>. If <paramref name="endpoints"/> is empty, the candidate will be marked
    /// as invalid.
    /// </param>
    /// <param name="comparer">
    /// The endpoint comparer used to order the endpoints. Can be retrieved from the service provider as
    /// type <see cref="EndpointMetadataComparer{TRequestContext}"/>.
    /// </param>
    /// <remarks>
    /// <para>
    /// This method supports replacing a dynamic endpoint with a collection of endpoints, and relying on
    /// <see cref="IEndpointSelectorPolicy{TRequestContext}"/> implementations to disambiguate further.
    /// </para>
    /// <para>
    /// The endpoint being replace should have a unique score value. The score is the combination of route
    /// patter precedence, order, and policy metadata evaluation. A dynamic endpoint will not function
    /// correctly if other endpoints exist with the same score.
    /// </para>
    /// </remarks>
    public void ExpandEndpoint(int index, IReadOnlyList<Endpoint<TRequestContext>> endpoints, IComparer<Endpoint<TRequestContext>> comparer)
    {
        // Friendliness for inlining
        if ((uint)index >= Count)
        {
            ThrowIndexArgumentOutOfRangeException();
        }

        if (endpoints == null)
        {
            ThrowArgumentNullException(nameof(endpoints));
        }

        if (comparer == null)
        {
            ThrowArgumentNullException(nameof(comparer));
        }

        // First we need to verify that the score of what we're replacing is unique.
        ValidateUniqueScore(index);

        switch (endpoints.Count)
        {
            case 0:
                ReplaceEndpoint(index, null, null);
                break;

            case 1:
                ReplaceEndpoint(index, endpoints[0], Candidates[index].Values);
                break;

            default:

                var score = GetOriginalScore(index);
                var values = Candidates[index].Values;

                // Adding candidates requires expanding the array and computing new score values for the new candidates.
                var original = Candidates;
                var candidates = new CandidateState<TRequestContext>[original.Length - 1 + endpoints.Count];
                Candidates = candidates;

                // Since the new endpoints have an unknown ordering relationship to each other, we need to:
                // - order them
                // - assign scores
                // - offset everything that comes after
                //
                // If the inputs look like:
                //
                // score 0: A1
                // score 0: A2
                // score 1: B
                // score 2: C <-- being expanded
                // score 3: D
                //
                // Then the result should look like:
                //
                // score 0: A1
                // score 0: A2
                // score 1: B
                // score 2: `C1
                // score 3: `C2
                // score 4: D

                // Candidates before index can be copied unchanged.
                for (var i = 0; i < index; i++)
                {
                    candidates[i] = original[i];
                }

                var buffer = endpoints.ToArray();
                Array.Sort<Endpoint<TRequestContext>>(buffer, comparer);

                // Add the first new endpoint with the current score
                candidates[index] = new CandidateState<TRequestContext>(buffer[0], values, score);

                var scoreOffset = 0;
                for (var i = 1; i < buffer.Length; i++)
                {
                    var cmp = comparer.Compare(buffer[i - 1], buffer[i]);

                    // This should not be possible. This would mean that sorting is wrong.
                    Debug.Assert(cmp <= 0);
                    if (cmp == 0)
                    {
                        // Score is unchanged.
                    }
                    else if (cmp < 0)
                    {
                        // Endpoint is lower priority, higher score.
                        scoreOffset++;
                    }

                    Candidates[i + index] = new CandidateState<TRequestContext>(buffer[i], values, score + scoreOffset);
                }

                for (var i = index + 1; i < original.Length; i++)
                {
                    Candidates[i + endpoints.Count - 1] = new CandidateState<TRequestContext>(original[i].Endpoint, original[i].Values, original[i].Score + scoreOffset);
                }

                break;
        }
    }

    // Returns the *positive* score value. Score is used to track valid/invalid which can cause it to be negative.
    //
    // This is the original score and used to determine if there are ambiguities.
    private int GetOriginalScore(int index)
    {
        var score = Candidates[index].Score;
        return score >= 0 ? score : ~score;
    }

    private void ValidateUniqueScore(int index)
    {
        var score = GetOriginalScore(index);

        var count = 0;
        var candidates = Candidates;
        for (var i = 0; i < candidates.Length; i++)
        {
            if (GetOriginalScore(i) == score)
            {
                count++;
            }
        }

        Debug.Assert(count > 0);
        if (count > 1)
        {
            // Uh-oh. We don't allow duplicates with ExpandEndpoint because that will do unpredictable things.
            var duplicates = new List<Endpoint<TRequestContext>>();
            for (var i = 0; i < candidates.Length; i++)
            {
                if (GetOriginalScore(i) == score)
                {
                    duplicates.Add(candidates[i].Endpoint!);
                }
            }

            var message =
                $"Using {nameof(ExpandEndpoint)} requires that the replaced endpoint have a unique priority. " +
                $"The following endpoints were found with the same priority:" + Environment.NewLine +
                string.Join(Environment.NewLine, duplicates.Select(e => e.DisplayName));
            throw new InvalidOperationException(message);
        }
    }

    [DoesNotReturn]
    private static void ThrowIndexArgumentOutOfRangeException()
    {
        throw new ArgumentOutOfRangeException("index");
    }

    [DoesNotReturn]
    private static void ThrowArgumentNullException(string parameter)
    {
        throw new ArgumentNullException(parameter);
    }
}