// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Patterns/RoutePatternLiteralPart.cs
// Source Sha256: 1494739c7347a5fc6330651e74442d733a7a53a2

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Pipeware.Routing.Patterns;

/// <summary>
/// Represents a literal text part of a route pattern. Instances of <see cref="RoutePatternLiteralPart"/>
/// are immutable.
/// </summary>
[DebuggerDisplay("{DebuggerToString()}")]
#if !COMPONENTS
public sealed class RoutePatternLiteralPart : RoutePatternPart
#else
internal sealed class RoutePatternLiteralPart : RoutePatternPart
#endif
{
    internal RoutePatternLiteralPart(string content)
        : base(RoutePatternPartKind.Literal)
    {
        Debug.Assert(!string.IsNullOrEmpty(content));
        Content = content;
    }

    /// <summary>
    /// Gets the text content.
    /// </summary>
    public string Content { get; }

    internal override string DebuggerToString()
    {
        return Content;
    }
}