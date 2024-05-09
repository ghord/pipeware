// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/ParameterPolicyFactory.cs
// Source Sha256: 13c46a5fd20c8151ca9f08674817587466d1a7ba

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Pipeware.Routing.Patterns;

namespace Pipeware.Routing;

/// <summary>
/// Defines an abstraction for resolving inline parameter policies as instances of <see cref="IParameterPolicy"/>.
/// </summary>
public abstract class ParameterPolicyFactory<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Creates a parameter policy.
    /// </summary>
    /// <param name="parameter">The parameter the parameter policy is being created for.</param>
    /// <param name="inlineText">The inline text to resolve.</param>
    /// <returns>The <see cref="IParameterPolicy"/> for the parameter.</returns>
    public abstract IParameterPolicy Create(RoutePatternParameterPart? parameter, string inlineText);

    /// <summary>
    /// Creates a parameter policy.
    /// </summary>
    /// <param name="parameter">The parameter the parameter policy is being created for.</param>
    /// <param name="parameterPolicy">An existing parameter policy.</param>
    /// <returns>The <see cref="IParameterPolicy"/> for the parameter.</returns>
    public abstract IParameterPolicy Create(RoutePatternParameterPart? parameter, IParameterPolicy parameterPolicy);

    /// <summary>
    /// Creates a parameter policy.
    /// </summary>
    /// <param name="parameter">The parameter the parameter policy is being created for.</param>
    /// <param name="reference">The reference to resolve.</param>
    /// <returns>The <see cref="IParameterPolicy"/> for the parameter.</returns>
    public IParameterPolicy Create(RoutePatternParameterPart? parameter, RoutePatternParameterPolicyReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);

        Debug.Assert(reference.ParameterPolicy != null || reference.Content != null);

        if (reference.ParameterPolicy != null)
        {
            return Create(parameter, reference.ParameterPolicy);
        }

        if (reference.Content != null)
        {
            return Create(parameter, reference.Content);
        }

        // Unreachable
        throw new NotSupportedException();
    }
}