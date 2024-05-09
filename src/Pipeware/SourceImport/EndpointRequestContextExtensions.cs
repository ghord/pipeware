// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/Routing/EndpointHttpContextExtensions.cs
// Source Sha256: dc309201494fdfa6b714fcbb75e5f7fb9bdb86ab

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware.Features;

namespace Pipeware;

/// <summary>
/// Extension methods to expose Endpoint on HttpContext.
/// </summary>
public static class EndpointRequestContextExtensions
{
    /// <summary>
    /// Extension method for getting the <see cref="Endpoint{TRequestContext}"/> for the current request.
    /// </summary>
    /// <param name="context">The <see cref="TRequestContext"/> context.</param>
    /// <returns>The <see cref="Endpoint{TRequestContext}"/>.</returns>
    public static Endpoint<TRequestContext>? GetEndpoint<TRequestContext>(this TRequestContext context) where TRequestContext : class, IRequestContext
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Features.Get<IEndpointFeature<TRequestContext>>()?.Endpoint;
    }

    /// <summary>
    /// Extension method for setting the <see cref="Endpoint{TRequestContext}"/> for the current request.
    /// </summary>
    /// <param name="context">The <see cref="TRequestContext"/> context.</param>
    /// <param name="endpoint">The <see cref="Endpoint{TRequestContext}"/>.</param>
    public static void SetEndpoint<TRequestContext>(this TRequestContext context, Endpoint<TRequestContext>? endpoint) where TRequestContext : class, IRequestContext
    {
        ArgumentNullException.ThrowIfNull(context);

        var feature = context.Features.Get<IEndpointFeature<TRequestContext>>();

        if (endpoint != null)
        {
            if (feature == null)
            {
                feature = new EndpointFeature<TRequestContext>();
                context.Features.Set(feature);
            }

            feature.Endpoint = endpoint;
        }
        else
        {
            if (feature == null)
            {
                // No endpoint to set and no feature on context. Do nothing
                return;
            }

            feature.Endpoint = null;
        }
    }

    private sealed class EndpointFeature<TRequestContext> : IEndpointFeature<TRequestContext> where TRequestContext : class, IRequestContext
    {
        public Endpoint<TRequestContext>? Endpoint { get; set; }
    }
}