// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Patterns/RoutePatternPart.cs
// Source Sha256: 71be36cddc5de40ca9b37409afa80f49fe7bf4ad

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Routing.Patterns;

/// <summary>
/// Represents a part of a route pattern.
/// </summary>
#if !COMPONENTS
public abstract class RoutePatternPart
#else
internal abstract class RoutePatternPart
#endif
{
    // This class is **not** an extensibility point - every part of the routing system
    // needs to be aware of what kind of parts we support.
    //
    // It is abstract so we can add semantics later inside the library.
    private protected RoutePatternPart(RoutePatternPartKind partKind)
    {
        PartKind = partKind;
    }

    /// <summary>
    /// Gets the <see cref="RoutePatternPartKind"/> of this part.
    /// </summary>
    public RoutePatternPartKind PartKind { get; }

    /// <summary>
    /// Returns <c>true</c> if this part is literal text. Otherwise returns <c>false</c>.
    /// </summary>
    public bool IsLiteral => PartKind == RoutePatternPartKind.Literal;

    /// <summary>
    /// Returns <c>true</c> if this part is a route parameter. Otherwise returns <c>false</c>.
    /// </summary>
    public bool IsParameter => PartKind == RoutePatternPartKind.Parameter;

    /// <summary>
    /// Returns <c>true</c> if this part is an optional separator. Otherwise returns <c>false</c>.
    /// </summary>
    public bool IsSeparator => PartKind == RoutePatternPartKind.Separator;

    internal abstract string DebuggerToString();
}