// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/AmbiguousMatchException.cs
// Source Sha256: 37df8ef49ef463946af8f1a0ce8b525716e40dd9

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace Pipeware.Routing.Matching;

/// <summary>
/// An exception which indicates multiple matches in endpoint selection.
/// </summary>
[Serializable]
internal sealed class AmbiguousMatchException : Exception
{
    public AmbiguousMatchException(string message)
        : base(message)
    {
    }

    [Obsolete]
    internal AmbiguousMatchException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}