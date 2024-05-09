// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Constraints/RegexInlineRouteConstraint.cs
// Source Sha256: a70bc36e5016873e65e0c01bce6a709dfc47ad72

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
#if !COMPONENTS
using Pipeware.Routing.Matching;
#endif

namespace Pipeware.Routing.Constraints;

#if !COMPONENTS
/// <summary>
/// Represents a regex constraint which can be used as an inlineConstraint.
/// </summary>
public class RegexInlineRouteConstraint : RegexRouteConstraint, ICachableParameterPolicy
#else
internal class RegexInlineRouteConstraint : RegexRouteConstraint
#endif
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegexInlineRouteConstraint" /> class.
    /// </summary>
    /// <param name="regexPattern">The regular expression pattern to match.</param>
    public RegexInlineRouteConstraint([StringSyntax(StringSyntaxAttribute.Regex, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)] string regexPattern)
        : base(regexPattern)
    {
    }
}