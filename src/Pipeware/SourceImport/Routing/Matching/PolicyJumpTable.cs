// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/PolicyJumpTable.cs
// Source Sha256: ba98dfb039d5edd4302d39fd11937acf457cc672

// Originally licensed under:

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;

namespace Pipeware.Routing.Matching;

/// <summary>
/// Supports retrieving endpoints that fulfill a certain matcher policy.
/// </summary>
public abstract class PolicyJumpTable<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Returns the destination for a given <paramref name="httpContext"/> in the current jump table.
    /// </summary>
    /// <param name="httpContext">The <see cref="TRequestContext"/> associated with the current request.</param>
    public abstract int GetDestination(TRequestContext requestContext);

    internal virtual string DebuggerToString()
    {
        return GetType().Name;
    }
}