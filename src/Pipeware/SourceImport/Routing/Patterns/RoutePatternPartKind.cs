// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Patterns/RoutePatternPartKind.cs
// Source Sha256: b4dff5c18b58e33f112af172d7c6deddf056481a

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Routing.Patterns;

/// <summary>
/// Defines the kinds of <see cref="RoutePatternPart"/> instances.
/// </summary>
#if !COMPONENTS
public enum RoutePatternPartKind
#else
internal enum RoutePatternPartKind
#endif
{
    /// <summary>
    /// The <see cref="RoutePatternPartKind"/> of a <see cref="RoutePatternLiteralPart"/>.
    /// </summary>
    Literal,

    /// <summary>
    /// The <see cref="RoutePatternPartKind"/> of a <see cref="RoutePatternParameterPart"/>.
    /// </summary>
    Parameter,

    /// <summary>
    /// The <see cref="RoutePatternPartKind"/> of a <see cref="RoutePatternSeparatorPart"/>.
    /// </summary>
    Separator,
}