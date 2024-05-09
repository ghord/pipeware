// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/EndpointComparer.cs
// Source Sha256: 88f0714e1886ad5c9fcd695105866f125aae07ac

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Pipeware;

namespace Pipeware.Routing.Matching;

// Use to sort and group Endpoints. RouteEndpoints are sorted before other implementations.
//
// NOTE:
// When ordering endpoints, we compare the route templates as an absolute last resort.
// This is used as a factor to ensure that we always have a predictable ordering
// for tests, errors, etc.
//
// When we group endpoints we don't consider the route template, because we're trying
// to group endpoints not separate them.
//
// TLDR:
//  IComparer implementation considers the template string as a tiebreaker.
//  IEqualityComparer implementation does not.
//  This is cool and good.
internal sealed class EndpointComparer<TRequestContext> : IComparer<Endpoint<TRequestContext>>, IEqualityComparer<Endpoint<TRequestContext>> where TRequestContext : class, IRequestContext
{
    private readonly IComparer<Endpoint<TRequestContext>>[] _comparers;

    public EndpointComparer(IEndpointComparerPolicy<TRequestContext>[] policies)
    {
        // Order, Precedence, (others)...
        _comparers = new IComparer<Endpoint<TRequestContext>>[2 + policies.Length];
        _comparers[0] = OrderComparer.Instance;
        _comparers[1] = PrecedenceComparer.Instance;
        for (var i = 0; i < policies.Length; i++)
        {
            _comparers[i + 2] = policies[i].Comparer;
        }
    }

    public int Compare(Endpoint<TRequestContext>? x, Endpoint<TRequestContext>? y)
    {
        // We don't expose this publicly, and we should never call it on
        // a null endpoint.
        Debug.Assert(x != null);
        Debug.Assert(y != null);

        var compare = CompareCore(x, y);

        // Since we're sorting, use the route template as a last resort.
        return compare == 0 ? ComparePattern(x, y) : compare;
    }

    private static int ComparePattern(Endpoint<TRequestContext> x, Endpoint<TRequestContext> y)
    {
        // A RouteEndpoint always comes before a non-RouteEndpoint, regardless of its RawText value
        var routeEndpointX = x as RouteEndpoint<TRequestContext>;
        var routeEndpointY = y as RouteEndpoint<TRequestContext>;

        if (routeEndpointX != null)
        {
            if (routeEndpointY != null)
            {
                return string.Compare(routeEndpointX.RoutePattern.RawText, routeEndpointY.RoutePattern.RawText, StringComparison.OrdinalIgnoreCase);
            }

            return 1;
        }
        else if (routeEndpointY != null)
        {
            return -1;
        }

        return 0;
    }

    public bool Equals(Endpoint<TRequestContext>? x, Endpoint<TRequestContext>? y)
    {
        // We don't expose this publicly, and we should never call it on
        // a null endpoint.
        Debug.Assert(x != null);
        Debug.Assert(y != null);

        return CompareCore(x, y) == 0;
    }

    public int GetHashCode(Endpoint<TRequestContext> obj)
    {
        // This should not be possible to call publicly.
        Debug.Fail("We don't expect this to be called.");
        throw new System.NotImplementedException();
    }

    private int CompareCore(Endpoint<TRequestContext> x, Endpoint<TRequestContext> y)
    {
        for (var i = 0; i < _comparers.Length; i++)
        {
            var compare = _comparers[i].Compare(x, y);
            if (compare != 0)
            {
                return compare;
            }
        }

        return 0;
    }

    private sealed class OrderComparer : IComparer<Endpoint<TRequestContext>>
    {
        public static readonly IComparer<Endpoint<TRequestContext>> Instance = new OrderComparer();

        public int Compare(Endpoint<TRequestContext>? x, Endpoint<TRequestContext>? y)
        {
            var routeEndpointX = x as RouteEndpoint<TRequestContext>;
            var routeEndpointY = y as RouteEndpoint<TRequestContext>;

            if (routeEndpointX != null)
            {
                if (routeEndpointY != null)
                {
                    return routeEndpointX.Order.CompareTo(routeEndpointY.Order);
                }

                return 1;
            }
            else if (routeEndpointY != null)
            {
                return -1;
            }

            return 0;
        }
    }

    private sealed class PrecedenceComparer : IComparer<Endpoint<TRequestContext>>
    {
        public static readonly IComparer<Endpoint<TRequestContext>> Instance = new PrecedenceComparer();

        public int Compare(Endpoint<TRequestContext>? x, Endpoint<TRequestContext>? y)
        {
            var routeEndpointX = x as RouteEndpoint<TRequestContext>;
            var routeEndpointY = y as RouteEndpoint<TRequestContext>;

            if (routeEndpointX != null)
            {
                if (routeEndpointY != null)
                {
                    return routeEndpointX.RoutePattern.InboundPrecedence
                        .CompareTo(routeEndpointY.RoutePattern.InboundPrecedence);
                }

                return 1;
            }
            else if (routeEndpointY != null)
            {
                return -1;
            }

            return 0;
        }
    }
}