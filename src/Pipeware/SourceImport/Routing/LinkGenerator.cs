// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing.Abstractions/src/LinkGenerator.cs
// Source Sha256: 2546e031dc8600f16fa7948f924260178818b048

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Pipeware;

namespace Pipeware.Routing;

/// <summary>
/// Defines a contract to generate absolute and related URIs based on endpoint routing.
/// </summary>
/// <remarks>
/// <para>
/// Generating URIs in endpoint routing occurs in two phases. First, an address is bound to a list of
/// endpoints that match the address. Secondly, each endpoint's <c>RoutePattern</c> is evaluated, until
/// a route pattern that matches the supplied values is found. The resulting output is combined with
/// the other URI parts supplied to the link generator and returned.
/// </para>
/// <para>
/// The methods provided by the <see cref="LinkGenerator{TRequestContext}"/> type are general infrastructure, and support
/// the standard link generator functionality for any type of address. The most convenient way to use
/// <see cref="LinkGenerator{TRequestContext}"/> is through extension methods that perform operations for a specific
/// address type.
/// </para>
/// </remarks>
public abstract class LinkGenerator<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Generates a URI with an absolute path based on the provided values and <see cref="TRequestContext"/>.
    /// </summary>
    /// <typeparam name="TAddress">The address type.</typeparam>
    /// <param name="httpContext">The <see cref="TRequestContext"/> associated with the current request.</param>
    /// <param name="address">The address value. Used to resolve endpoints.</param>
    /// <param name="values">The route values. Used to expand parameters in the route template.</param>
    /// <param name="ambientValues">The values associated with the current request. Optional.</param>
    /// <param name="pathBase">
    /// An optional URI path base. Prepended to the path in the resulting URI. If not provided, the value of <see cref="HttpRequest.PathBase"/> will be used.
    /// </param>
    /// <param name="fragment">An optional URI fragment. Appended to the resulting URI.</param>
    /// <param name="options">
    /// An optional <see cref="LinkOptions"/>. Settings on provided object override the settings with matching
    /// names from <c>RouteOptions</c>.
    /// </param>
    /// <returns>A URI with an absolute path, or <c>null</c>.</returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public abstract string? GetPathByAddress<TAddress>(
        TRequestContext requestContext,
        TAddress address,
        RouteValueDictionary values,
        RouteValueDictionary? ambientValues = default,
        PathString? pathBase = default,
        FragmentString fragment = default,
        LinkOptions? options = default);

    /// <summary>
    /// Generates a URI with an absolute path based on the provided values.
    /// </summary>
    /// <typeparam name="TAddress">The address type.</typeparam>
    /// <param name="address">The address value. Used to resolve endpoints.</param>
    /// <param name="values">The route values. Used to expand parameters in the route template.</param>
    /// <param name="pathBase">An optional URI path base. Prepended to the path in the resulting URI.</param>
    /// <param name="fragment">An optional URI fragment. Appended to the resulting URI.</param>
    /// <param name="options">
    /// An optional <see cref="LinkOptions"/>. Settings on provided object override the settings with matching
    /// names from <c>RouteOptions</c>.
    /// </param>
    /// <returns>A URI with an absolute path, or <c>null</c>.</returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public abstract string? GetPathByAddress<TAddress>(
        TAddress address,
        RouteValueDictionary values,
        PathString pathBase = default,
        FragmentString fragment = default,
        LinkOptions? options = default);
}