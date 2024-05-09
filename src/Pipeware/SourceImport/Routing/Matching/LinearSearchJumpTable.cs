// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/LinearSearchJumpTable.cs
// Source Sha256: c604c7c53f13ac16cec929672321940685be3391

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;

namespace Pipeware.Routing.Matching;

internal sealed class LinearSearchJumpTable : JumpTable
{
    private readonly int _defaultDestination;
    private readonly int _exitDestination;
    private readonly (string text, int destination)[] _entries;

    public LinearSearchJumpTable(
        int defaultDestination,
        int exitDestination,
        (string text, int destination)[] entries)
    {
        _defaultDestination = defaultDestination;
        _exitDestination = exitDestination;
        _entries = entries;
    }

    public override int GetDestination(string path, PathSegment segment)
    {
        if (segment.Length == 0)
        {
            return _exitDestination;
        }

        var entries = _entries;
        for (var i = 0; i < entries.Length; i++)
        {
            var text = entries[i].text;
            if (segment.Length == text.Length &&
                string.Compare(
                    path,
                    segment.Start,
                    text,
                    0,
                    segment.Length,
                    StringComparison.OrdinalIgnoreCase) == 0)
            {
                return entries[i].destination;
            }
        }

        return _defaultDestination;
    }

    public override string DebuggerToString()
    {
        var builder = new StringBuilder();
        builder.Append("{ ");

        builder.AppendJoin(", ", _entries.Select(e => $"{e.text}: {e.destination}"));

        builder.Append("$+: ");
        builder.Append(_defaultDestination);
        builder.Append(", ");

        builder.Append("$0: ");
        builder.Append(_defaultDestination);

        builder.Append(" }");

        return builder.ToString();
    }
}