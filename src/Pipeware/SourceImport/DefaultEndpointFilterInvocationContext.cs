// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/DefaultEndpointFilterInvocationContext.cs
// Source Sha256: a44d726481d75c1f78c135edb76905c029f1dd4e

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware;

/// <summary>
/// Provides a default implementation for wrapping the <see cref="TRequestContext"/> and parameters
/// provided to a route handler.
/// </summary>
public sealed class DefaultEndpointFilterInvocationContext<TRequestContext> : EndpointFilterInvocationContext<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Creates a new instance of the <see cref="DefaultEndpointFilterInvocationContext{TRequestContext}"/> for a given request.
    /// </summary>
    /// <param name="httpContext">The <see cref="TRequestContext"/> associated with the current request.</param>
    /// <param name="arguments">A list of parameters provided in the current request.</param>
    public DefaultEndpointFilterInvocationContext(TRequestContext requestContext, params object?[] arguments)
    {
        RequestContext = requestContext;
        Arguments = arguments;
    }

    /// <inheritdoc />
    public override TRequestContext RequestContext { get; }

    /// <inheritdoc />
    public override IList<object?> Arguments { get; }

    /// <inheritdoc />
    public override T GetArgument<T>(int index)
    {
        return (T)Arguments[index]!;
    }
}