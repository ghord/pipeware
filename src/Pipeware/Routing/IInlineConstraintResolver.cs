// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: src/Http/Routing/src/IInlineConstraintResolver.cs

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Routing;

/// <summary>
/// Defines an marker interface for resolving inline constraints for specific type of <see cref="IRouteConstraint"/>.
/// </summary>
public interface IInlineConstraintResolver<TRequestContext> : IInlineConstraintResolver where TRequestContext : class, IRequestContext
{

}