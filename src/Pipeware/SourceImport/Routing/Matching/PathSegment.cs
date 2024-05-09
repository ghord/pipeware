// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/PathSegment.cs
// Source Sha256: 5841f9243b3bc2f4fd56f97306663bb61bcc4e1d

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Routing.Matching;

internal readonly struct PathSegment : IEquatable<PathSegment>
{
    public readonly int Start;
    public readonly int Length;

    public PathSegment(int start, int length)
    {
        Start = start;
        Length = length;
    }

    public override bool Equals(object? obj)
    {
        return obj is PathSegment segment ? Equals(segment) : false;
    }

    public bool Equals(PathSegment other)
    {
        return Start == other.Start && Length == other.Length;
    }

    public override int GetHashCode()
    {
        return Start;
    }

    public override string ToString()
    {
        return $"Segment({Start}:{Length})";
    }
}