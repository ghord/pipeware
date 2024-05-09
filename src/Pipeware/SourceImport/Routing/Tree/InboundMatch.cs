// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Tree/InboundMatch.cs
// Source Sha256: a8c18de91defb0509143582728af63c717fee61c

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Diagnostics;
#if !COMPONENTS
using Pipeware.Routing.Template;
#endif

namespace Pipeware.Routing.Tree;

/// <summary>
/// A candidate route to match incoming URLs in a <see cref="TreeRouter"/>.
/// </summary>
[DebuggerDisplay("{DebuggerToString(),nq}")]
#if !COMPONENTS
public class InboundMatch
#else
internal class InboundMatch
#endif
{
    /// <summary>
    /// Gets or sets the <see cref="InboundRouteEntry"/>.
    /// </summary>
    public InboundRouteEntry Entry { get; set; }

#if !COMPONENTS
    /// <summary>
    /// Gets or sets the <see cref="TemplateMatcher"/>.
    /// </summary>
    public TemplateMatcher TemplateMatcher { get; set; }
#else
    public RoutePatternMatcher TemplateMatcher { get; set; }
#endif

    private string DebuggerToString()
    {
#if !COMPONENTS
        return TemplateMatcher?.Template?.TemplateText;
#else
        return TemplateMatcher?.RoutePattern?.RawText;
#endif
    }
}