// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Tree/OutboundMatchResult.cs
// Source Sha256: db1d31a9eb9a5d18b7526ac52d2b45988157d04d

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Routing.Tree;

internal readonly struct OutboundMatchResult
{
    public OutboundMatchResult(OutboundMatch match, bool isFallbackMatch)
    {
        Match = match;
        IsFallbackMatch = isFallbackMatch;
    }

    public OutboundMatch Match { get; }

    public bool IsFallbackMatch { get; }
}