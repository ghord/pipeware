// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/IInlineConstraintResolver.cs
// Source Sha256: b298bef235a8cde62502db898c5a8d367053e099

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Routing;

#if !COMPONENTS
/// <summary>
/// Defines an abstraction for resolving inline constraints as instances of <see cref="IRouteConstraint"/>.
/// </summary>
public interface IInlineConstraintResolver
#else
internal interface IInlineConstraintResolver
#endif
{
    /// <summary>
    /// Resolves the inline constraint.
    /// </summary>
    /// <param name="inlineConstraint">The inline constraint to resolve.</param>
    /// <returns>The <see cref="IRouteConstraint"/> the inline constraint was resolved to.</returns>
    IRouteConstraint? ResolveConstraint(string inlineConstraint);
}