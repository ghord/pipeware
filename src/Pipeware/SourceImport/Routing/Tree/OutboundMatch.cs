// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Tree/OutboundMatch.cs
// Source Sha256: eef5a421db2852fa82aa14b779bdd1cb5c45e406

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using Pipeware.Routing.Template;

namespace Pipeware.Routing.Tree;

/// <summary>
/// A candidate match for link generation in a <see cref="TreeRouter"/>.
/// </summary>
public class OutboundMatch
{
    /// <summary>
    /// Gets or sets the <see cref="OutboundRouteEntry"/>.
    /// </summary>
    public OutboundRouteEntry Entry { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="TemplateBinder"/>.
    /// </summary>
    public TemplateBinder TemplateBinder { get; set; }
}