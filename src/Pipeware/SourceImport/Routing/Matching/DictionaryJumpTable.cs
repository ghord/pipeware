// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/DictionaryJumpTable.cs
// Source Sha256: 18c7754bf83ca2f73855102733d2e277015c5d38

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Linq;
using System.Text;

namespace Pipeware.Routing.Matching;

internal sealed class DictionaryJumpTable : JumpTable
{
    private readonly int _defaultDestination;
    private readonly int _exitDestination;
    private readonly FrozenDictionary<string, int> _dictionary;

    public DictionaryJumpTable(
        int defaultDestination,
        int exitDestination,
        (string text, int destination)[] entries)
    {
        _defaultDestination = defaultDestination;
        _exitDestination = exitDestination;

        _dictionary = entries.ToFrozenDictionary(e => e.text, e => e.destination, StringComparer.OrdinalIgnoreCase);
    }

    public override int GetDestination(string path, PathSegment segment)
    {
        if (segment.Length == 0)
        {
            return _exitDestination;
        }

        var text = path.Substring(segment.Start, segment.Length);
        if (_dictionary.TryGetValue(text, out var destination))
        {
            return destination;
        }

        return _defaultDestination;
    }

    public override string DebuggerToString()
    {
        var builder = new StringBuilder();
        builder.Append("{ ");

        builder.AppendJoin(", ", _dictionary.Select(kvp => $"{kvp.Key}: {kvp.Value}"));

        builder.Append("$+: ");
        builder.Append(_defaultDestination);
        builder.Append(", ");

        builder.Append("$0: ");
        builder.Append(_defaultDestination);

        builder.Append(" }");

        return builder.ToString();
    }
}