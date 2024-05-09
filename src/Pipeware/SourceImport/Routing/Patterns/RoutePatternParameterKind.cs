// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Patterns/RoutePatternParameterKind.cs
// Source Sha256: 0024de804b907cd41daeff25173fce19163141c4

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Routing.Patterns;

#if !COMPONENTS
/// <summary>
/// Defines the kinds of <see cref="RoutePatternParameterPart"/> instances.
/// </summary>
public enum RoutePatternParameterKind
#else
internal enum RoutePatternParameterKind
#endif
{
    /// <summary>
    /// The <see cref="RoutePatternParameterKind"/> of a standard parameter
    /// without optional or catch all behavior.
    /// </summary>
    Standard,

    /// <summary>
    /// The <see cref="RoutePatternParameterKind"/> of an optional parameter.
    /// </summary>
    Optional,

    /// <summary>
    /// The <see cref="RoutePatternParameterKind"/> of a catch-all parameter.
    /// </summary>
    CatchAll,
}