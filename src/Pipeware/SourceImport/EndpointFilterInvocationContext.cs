// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/EndpointFilterInvocationContext.cs
// Source Sha256: 85508fee1baad11cdb6e85710ad5cb21e24a6eb9

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware;

/// <summary>
/// Provides an abstraction for wrapping the <see cref="TRequestContext"/> and arguments
/// provided to a route handler.
/// </summary>
public abstract class EndpointFilterInvocationContext<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// The <see cref="TRequestContext"/> associated with the current request being processed by the filter.
    /// </summary>
    public abstract TRequestContext RequestContext { get; }

    /// <summary>
    /// A list of arguments provided in the current request to the filter.
    /// <remarks>
    /// This list is not read-only to permit modifying of existing arguments by filters.
    /// </remarks>
    /// </summary>
    public abstract IList<object?> Arguments { get; }

    /// <summary>
    /// Retrieve the argument given its position in the argument list.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> of the resolved argument.</typeparam>
    /// <param name="index">An integer representing the position of the argument in the argument list.</param>
    /// <returns>The argument at a given <paramref name="index"/>.</returns>
    public abstract T GetArgument<T>(int index);

    /// <summary>
    /// Creates a strongly-typed implementation of a <see cref="EndpointFilterInvocationContext{TRequestContext}"/>
    /// given the provided type parameters.
    /// </summary>
    public static EndpointFilterInvocationContext<TRequestContext> Create(TRequestContext requestContext) =>
        new DefaultEndpointFilterInvocationContext<TRequestContext>(requestContext);

    /// <summary>
    /// Creates a strongly-typed implementation of a <see cref="EndpointFilterInvocationContext{TRequestContext}"/>
    /// given the provided type parameters.
    /// </summary>
    public static EndpointFilterInvocationContext<TRequestContext> Create<T>(TRequestContext requestContext, T arg) =>
        new EndpointFilterInvocationContext<T, TRequestContext>(requestContext, arg);

    /// <summary>
    /// Creates a strongly-typed implementation of a <see cref="EndpointFilterInvocationContext{TRequestContext}"/>
    /// given the provided type parameters.
    /// </summary>
    public static EndpointFilterInvocationContext<TRequestContext> Create<T1, T2>(TRequestContext requestContext, T1 arg1, T2 arg2) =>
        new EndpointFilterInvocationContext<T1, T2, TRequestContext>(requestContext, arg1, arg2);

    /// <summary>
    /// Creates a strongly-typed implementation of a <see cref="EndpointFilterInvocationContext{TRequestContext}"/>
    /// given the provided type parameters.
    /// </summary>
    public static EndpointFilterInvocationContext<TRequestContext> Create<T1, T2, T3>(TRequestContext requestContext, T1 arg1, T2 arg2, T3 arg3) =>
        new EndpointFilterInvocationContext<T1, T2, T3, TRequestContext>(requestContext, arg1, arg2, arg3);

    /// <summary>
    /// Creates a strongly-typed implementation of a <see cref="EndpointFilterInvocationContext{TRequestContext}"/>
    /// given the provided type parameters.
    /// </summary>
    public static EndpointFilterInvocationContext<TRequestContext> Create<T1, T2, T3, T4>(TRequestContext requestContext, T1 arg1, T2 arg2, T3 arg3, T4 arg4) =>
        new EndpointFilterInvocationContext<T1, T2, T3, T4, TRequestContext>(requestContext, arg1, arg2, arg3, arg4);

    /// <summary>
    /// Creates a strongly-typed implementation of a <see cref="EndpointFilterInvocationContext{TRequestContext}"/>
    /// given the provided type parameters.
    /// </summary>
    public static EndpointFilterInvocationContext<TRequestContext> Create<T1, T2, T3, T4, T5>(TRequestContext requestContext, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) =>
        new EndpointFilterInvocationContext<T1, T2, T3, T4, T5, TRequestContext>(requestContext, arg1, arg2, arg3, arg4, arg5);

    /// <summary>
    /// Creates a strongly-typed implementation of a <see cref="EndpointFilterInvocationContext{TRequestContext}"/>
    /// given the provided type parameters.
    /// </summary>
    public static EndpointFilterInvocationContext<TRequestContext> Create<T1, T2, T3, T4, T5, T6>(TRequestContext requestContext, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) =>
        new EndpointFilterInvocationContext<T1, T2, T3, T4, T5, T6, TRequestContext>(requestContext, arg1, arg2, arg3, arg4, arg5, arg6);

    /// <summary>
    /// Creates a strongly-typed implementation of a <see cref="EndpointFilterInvocationContext{TRequestContext}"/>
    /// given the provided type parameters.
    /// </summary>
    public static EndpointFilterInvocationContext<TRequestContext> Create<T1, T2, T3, T4, T5, T6, T7>(TRequestContext requestContext, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7) =>
        new EndpointFilterInvocationContext<T1, T2, T3, T4, T5, T6, T7, TRequestContext>(requestContext, arg1, arg2, arg3, arg4, arg5, arg6, arg7);

    /// <summary>
    /// Creates a strongly-typed implementation of a <see cref="EndpointFilterInvocationContext{TRequestContext}"/>
    /// given the provided type parameters.
    /// </summary>
    public static EndpointFilterInvocationContext<TRequestContext> Create<T1, T2, T3, T4, T5, T6, T7, T8>(TRequestContext requestContext, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8) =>
        new EndpointFilterInvocationContext<T1, T2, T3, T4, T5, T6, T7, T8, TRequestContext>(requestContext, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
}