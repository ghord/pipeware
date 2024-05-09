// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/ZeroEntryJumpTable.cs
// Source Sha256: 8d39712369aa53f0c9c10685c2f41f85b2e27fa6

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Routing.Matching;

internal sealed class ZeroEntryJumpTable : JumpTable
{
    private readonly int _defaultDestination;
    private readonly int _exitDestination;

    public ZeroEntryJumpTable(int defaultDestination, int exitDestination)
    {
        _defaultDestination = defaultDestination;
        _exitDestination = exitDestination;
    }

    public override int GetDestination(string path, PathSegment segment)
    {
        return segment.Length == 0 ? _exitDestination : _defaultDestination;
    }

    public override string DebuggerToString()
    {
        return $"{{ $+: {_defaultDestination}, $0: {_exitDestination} }}";
    }
}