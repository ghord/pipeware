// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Template/TemplateBinderFactory.cs
// Source Sha256: ad14113ddd0d07bbe7a8f89c3a6f8dd66f2c9afd

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware.Routing.Patterns;

namespace Pipeware.Routing.Template;

/// <summary>
/// A factory used to create <see cref="TemplateBinder"/> instances.
/// </summary>
public abstract class TemplateBinderFactory<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Creates a new <see cref="TemplateBinder"/> from the provided <paramref name="template"/> and
    /// <paramref name="defaults"/>.
    /// </summary>
    /// <param name="template">The route template.</param>
    /// <param name="defaults">A collection of extra default values that do not appear in the route template.</param>
    /// <returns>A <see cref="TemplateBinder"/>.</returns>
    public abstract TemplateBinder Create(RouteTemplate template, RouteValueDictionary defaults);

    /// <summary>
    /// Creates a new <see cref="TemplateBinder"/> from the provided <paramref name="pattern"/>.
    /// </summary>
    /// <param name="pattern">The <see cref="RoutePattern"/>.</param>
    /// <returns>A <see cref="TemplateBinder"/>.</returns>
    public abstract TemplateBinder Create(RoutePattern pattern);
}