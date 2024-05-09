// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/Extensions/MapWhenOptions.cs
// Source Sha256: 962acbd23e75717ef93dbf8f23fee6ecb837ebd3

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;

namespace Pipeware.Builder.Extensions;

/// <summary>
/// Options for the <see cref="MapWhenMiddleware{TRequestContext}"/>.
/// </summary>
public class MapWhenOptions<TRequestContext> where TRequestContext : class, IRequestContext
{
    private Func<TRequestContext, bool>? _predicate;

    /// <summary>
    /// The user callback that determines if the branch should be taken.
    /// </summary>
    public Func<TRequestContext, bool>? Predicate
    {
        get
        {
            return _predicate;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            _predicate = value;
        }
    }

    /// <summary>
    /// The branch taken for a positive match.
    /// </summary>
    public RequestDelegate<TRequestContext>? Branch { get; set; }
}