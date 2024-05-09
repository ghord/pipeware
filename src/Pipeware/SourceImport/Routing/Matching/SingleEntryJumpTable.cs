// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/SingleEntryJumpTable.cs
// Source Sha256: 4769c87b622d772ec8d014b33ded8cf208b211f8

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Routing.Matching;

internal sealed class SingleEntryJumpTable : JumpTable
{
    private readonly int _defaultDestination;
    private readonly int _exitDestination;
    private readonly string _text;
    private readonly int _destination;

    public SingleEntryJumpTable(
        int defaultDestination,
        int exitDestination,
        string text,
        int destination)
    {
        _defaultDestination = defaultDestination;
        _exitDestination = exitDestination;
        _text = text;
        _destination = destination;
    }

    public override int GetDestination(string path, PathSegment segment)
    {
        if (segment.Length == 0)
        {
            return _exitDestination;
        }

        if (segment.Length == _text.Length &&
            string.Compare(
                path,
                segment.Start,
                _text,
                0,
                segment.Length,
                StringComparison.OrdinalIgnoreCase) == 0)
        {
            return _destination;
        }

        return _defaultDestination;
    }

    public override string DebuggerToString()
    {
        return $"{{ {_text}: {_destination}, $+: {_defaultDestination}, $0: {_exitDestination} }}";
    }
}