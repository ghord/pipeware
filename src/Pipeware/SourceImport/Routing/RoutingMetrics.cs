// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/RoutingMetrics.cs
// Source Sha256: 7b1388b235bfe86d7dd44ce8a4e2bf85281a8a92

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;

namespace Pipeware.Routing;

internal sealed class RoutingMetrics
{
    public const string MeterName = "Microsoft.AspNetCore.Routing";

     // Reuse boxed object for common values
    private static readonly object BoxedTrue = true;
    private static readonly object BoxedFalse = false;

    private readonly Meter _meter;
    private readonly Counter<long> _matchAttemptsCounter;

    public RoutingMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _matchAttemptsCounter = _meter.CreateCounter<long>(
            "aspnetcore.routing.match_attempts",
            unit: "{match_attempt}",
            description: "Number of requests that were attempted to be matched to an endpoint.");
    }

    public bool MatchSuccessCounterEnabled => _matchAttemptsCounter.Enabled;

    public void MatchSuccess(string route, bool isFallback)
    {
        _matchAttemptsCounter.Add(1,
            new KeyValuePair<string, object?>("http.route", route),
            new KeyValuePair<string, object?>("aspnetcore.routing.match_status", "success"),
            new KeyValuePair<string, object?>("aspnetcore.routing.is_fallback", isFallback ? BoxedTrue : BoxedFalse));
    }

    public void MatchFailure()
    {
        _matchAttemptsCounter.Add(1,
            new KeyValuePair<string, object?>("aspnetcore.routing.match_status", "failure"));
    }
}