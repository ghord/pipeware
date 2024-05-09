// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Template/InlineConstraint.cs
// Source Sha256: fde5e4a3d65a936d4bbb5a20dadc42484c663326

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware.Routing.Patterns;

namespace Pipeware.Routing.Template;

/// <summary>
/// The parsed representation of an inline constraint in a route parameter.
/// </summary>
#if !COMPONENTS
public class InlineConstraint
#else
internal class InlineConstraint
#endif
{
    /// <summary>
    /// Creates a new instance of <see cref="InlineConstraint"/>.
    /// </summary>
    /// <param name="constraint">The constraint text.</param>
    public InlineConstraint(string constraint)
    {
        ArgumentNullException.ThrowIfNull(constraint);

        Constraint = constraint;
    }

    /// <summary>
    /// Creates a new <see cref="InlineConstraint"/> instance given a <see cref="RoutePatternParameterPolicyReference"/>.
    /// </summary>
    /// <param name="other">A <see cref="RoutePatternParameterPolicyReference"/> instance.</param>
    public InlineConstraint(RoutePatternParameterPolicyReference other)
    {
        ArgumentNullException.ThrowIfNull(other);

        Constraint = other.Content!;
    }

    /// <summary>
    /// Gets the constraint text.
    /// </summary>
    public string Constraint { get; }
}