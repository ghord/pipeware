// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/JumpTable.cs
// Source Sha256: db3a4fa4c9cf6887bf90e8ca9f3ad4d8f3a46ac0

// Originally licensed under:

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Pipeware.Routing.Matching;

[DebuggerDisplay("{DebuggerToString(),nq}")]
internal abstract class JumpTable
{
    public abstract int GetDestination(string path, PathSegment segment);

    public virtual string DebuggerToString()
    {
        return GetType().Name;
    }
}