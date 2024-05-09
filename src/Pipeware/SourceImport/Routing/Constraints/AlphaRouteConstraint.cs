// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Constraints/AlphaRouteConstraint.cs
// Source Sha256: d3009776165077d995e02ed382ddaf66dc3a52f0

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
#if !COMPONENTS
using Pipeware.Routing.Matching;
#endif

namespace Pipeware.Routing.Constraints;

#if !COMPONENTS
/// <summary>
/// Constrains a route parameter to contain only lowercase or uppercase letters A through Z in the English alphabet.
/// </summary>
public partial class AlphaRouteConstraint : RegexRouteConstraint, ICachableParameterPolicy
#else
internal partial class AlphaRouteConstraint : RegexRouteConstraint
#endif
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AlphaRouteConstraint" /> class.
    /// </summary>
    public AlphaRouteConstraint() : base(GetAlphaRouteRegex())
    {
    }

    [GeneratedRegex(@"^[A-Za-z]*$")]
    private static partial Regex GetAlphaRouteRegex();
}