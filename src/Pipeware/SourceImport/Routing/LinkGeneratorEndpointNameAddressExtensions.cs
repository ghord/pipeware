// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/LinkGeneratorEndpointNameAddressExtensions.cs
// Source Sha256: be520e200ac67675e403d8dd26a6e18174c64920

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Pipeware;
using Pipeware.Internal;

namespace Pipeware.Routing;

/// <summary>
/// Extension methods for using <see cref="LinkGenerator{TRequestContext}"/> with and endpoint name.
/// </summary>
public static class LinkGeneratorEndpointNameAddressExtensions
{
    /// <summary>
    /// Generates a URI with an absolute path based on the provided values.
    /// </summary>
    /// <param name="generator">The <see cref="LinkGenerator{TRequestContext}"/>.</param>
    /// <param name="httpContext">The <see cref="TRequestContext"/> associated with the current request.</param>
    /// <param name="endpointName">The endpoint name. Used to resolve endpoints.</param>
    /// <param name="values">The route values. Used to expand parameters in the route template. Optional.</param>
    /// <param name="pathBase">
    /// An optional URI path base. Prepended to the path in the resulting URI. If not provided, the value of <see cref="HttpRequest.PathBase"/> will be used.
    /// </param>
    /// <param name="fragment">An optional URI fragment. Appended to the resulting URI.</param>
    /// <param name="options">
    /// An optional <see cref="LinkOptions"/>. Settings on provided object override the settings with matching
    /// names from <c>RouteOptions</c>.
    /// </param>
    /// <returns>A URI with an absolute path, or <c>null</c>.</returns>
    [RequiresUnreferencedCode(RouteValueDictionaryTrimmerWarning.Warning)]
    /// <summary>
    /// Generates a URI with an absolute path based on the provided values.
    /// </summary>
    /// <param name="generator">The <see cref="LinkGenerator{TRequestContext}"/>.</param>
    /// <param name="httpContext">The <see cref="TRequestContext"/> associated with the current request.</param>
    /// <param name="endpointName">The endpoint name. Used to resolve endpoints.</param>
    /// <param name="values">The route values. Used to expand parameters in the route template. Optional.</param>
    /// <param name="pathBase">
    /// An optional URI path base. Prepended to the path in the resulting URI. If not provided, the value of <see cref="HttpRequest.PathBase"/> will be used.
    /// </param>
    /// <param name="fragment">An optional URI fragment. Appended to the resulting URI.</param>
    /// <param name="options">
    /// An optional <see cref="LinkOptions"/>. Settings on provided object override the settings with matching
    /// names from <c>RouteOptions</c>.
    /// </param>
    /// <returns>A URI with an absolute path, or <c>null</c>.</returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]    public static string? GetPathByName<TRequestContext>(
        this LinkGenerator<TRequestContext> generator,
        TRequestContext requestContext,
        string endpointName,
        object? values,
        PathString? pathBase = default,
        FragmentString fragment = default,
        LinkOptions? options = default) where TRequestContext : class, IRequestContext
    {
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(requestContext);
        ArgumentNullException.ThrowIfNull(endpointName);

        return generator.GetPathByAddress<string>(
            requestContext,
            endpointName,
            new RouteValueDictionary(values),
            ambientValues: null,
            pathBase,
            fragment,
            options);
    }

    /// <summary>
    /// Generates a URI with an absolute path based on the provided values.
    /// </summary>
    /// <param name="generator">The <see cref="LinkGenerator{TRequestContext}"/>.</param>
    /// <param name="httpContext">The <see cref="TRequestContext"/> associated with the current request.</param>
    /// <param name="endpointName">The endpoint name. Used to resolve endpoints.</param>
    /// <param name="values">The route values. Used to expand parameters in the route template. Optional.</param>
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
    public static string? GetPathByName<TRequestContext>(
        this LinkGenerator<TRequestContext> generator,
        TRequestContext requestContext,
        string endpointName,
        RouteValueDictionary? values = default,
        PathString? pathBase = default,
        FragmentString fragment = default,
        LinkOptions? options = default) where TRequestContext : class, IRequestContext
    {
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(requestContext);
        ArgumentNullException.ThrowIfNull(endpointName);

        return generator.GetPathByAddress<string>(
            requestContext,
            endpointName,
            values ?? new(),
            ambientValues: null,
            pathBase,
            fragment,
            options);
    }

    /// <summary>
    /// Generates a URI with an absolute path based on the provided values.
    /// </summary>
    /// <param name="generator">The <see cref="LinkGenerator{TRequestContext}"/>.</param>
    /// <param name="endpointName">The endpoint name. Used to resolve endpoints.</param>
    /// <param name="values">The route values. Used to expand parameters in the route template. Optional.</param>
    /// <param name="pathBase">An optional URI path base. Prepended to the path in the resulting URI.</param>
    /// <param name="fragment">An optional URI fragment. Appended to the resulting URI.</param>
    /// <param name="options">
    /// An optional <see cref="LinkOptions"/>. Settings on provided object override the settings with matching
    /// names from <c>RouteOptions</c>.
    /// </param>
    /// <returns>A URI with an absolute path, or <c>null</c>.</returns>
    [RequiresUnreferencedCode(RouteValueDictionaryTrimmerWarning.Warning)]

    /// <summary>
    /// Generates a URI with an absolute path based on the provided values.
    /// </summary>
    /// <param name="generator">The <see cref="LinkGenerator{TRequestContext}"/>.</param>
    /// <param name="endpointName">The endpoint name. Used to resolve endpoints.</param>
    /// <param name="values">The route values. Used to expand parameters in the route template. Optional.</param>
    /// <param name="pathBase">An optional URI path base. Prepended to the path in the resulting URI.</param>
    /// <param name="fragment">An optional URI fragment. Appended to the resulting URI.</param>
    /// <param name="options">
    /// An optional <see cref="LinkOptions"/>. Settings on provided object override the settings with matching
    /// names from <c>RouteOptions</c>.
    /// </param>
    /// <returns>A URI with an absolute path, or <c>null</c>.</returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]    public static string? GetPathByName<TRequestContext>(
        this LinkGenerator<TRequestContext> generator,
        string endpointName,
        object? values,
        PathString pathBase = default,
        FragmentString fragment = default,
        LinkOptions? options = default) where TRequestContext : class, IRequestContext
    {
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(endpointName);

        return generator.GetPathByAddress<string>(endpointName, new RouteValueDictionary(values), pathBase, fragment, options);
    }

    /// <summary>
    /// Generates a URI with an absolute path based on the provided values.
    /// </summary>
    /// <param name="generator">The <see cref="LinkGenerator{TRequestContext}"/>.</param>
    /// <param name="endpointName">The endpoint name. Used to resolve endpoints.</param>
    /// <param name="values">The route values. Used to expand parameters in the route template. Optional.</param>
    /// <param name="pathBase">An optional URI path base. Prepended to the path in the resulting URI.</param>
    /// <param name="fragment">An optional URI fragment. Appended to the resulting URI.</param>
    /// <param name="options">
    /// An optional <see cref="LinkOptions"/>. Settings on provided object override the settings with matching
    /// names from <c>RouteOptions</c>.
    /// </param>
    /// <returns>A URI with an absolute path, or <c>null</c>.</returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static string? GetPathByName<TRequestContext>(
        this LinkGenerator<TRequestContext> generator,
        string endpointName,
        RouteValueDictionary? values = default,
        PathString pathBase = default,
        FragmentString fragment = default,
        LinkOptions? options = default) where TRequestContext : class, IRequestContext
    {
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(endpointName);

        return generator.GetPathByAddress<string>(endpointName, values ?? new(), pathBase, fragment, options);
    }
}