// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/Extensions/UseMiddlewareExtensions.cs
// Source alias: sync
// Source Sha256: 3cdc16d821efea9d7ae9ab0e3571e0b8710e117d

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Pipeware;

using Pipeware.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Pipeware.Builder;

/// <summary>
/// Extension methods for adding typed middleware.
/// </summary>
public static partial class UseMiddlewareExtensions
{
    /// <summary>
    /// Adds a middleware type to the application's request pipeline.
    /// </summary>
    /// <param name="app">The <see cref="ISyncPipelineBuilder{TRequestContext}"/> instance.</param>
    /// <param name="middleware">The middleware type.</param>
    /// <param name="args">The arguments to pass to the middleware type instance's constructor.</param>
    /// <returns>The <see cref="ISyncPipelineBuilder{TRequestContext}"/> instance.</returns>
    public static ISyncPipelineBuilder<TRequestContext> UseMiddleware<TRequestContext>(
        this ISyncPipelineBuilder<TRequestContext> app,
        [DynamicallyAccessedMembers(MiddlewareAccessibility)] Type middleware,
        params object?[] args) where TRequestContext : class, IRequestContext
    {
        if (typeof(ISyncMiddleware<TRequestContext>).IsAssignableFrom(middleware))
        {
            // IMiddleware doesn't support passing args directly since it's
            // activated from the container
            if (args.Length > 0)
            {
                throw new NotSupportedException(string.Format("Types that implement '{0}' do not support explicit arguments.", typeof(ISyncMiddleware<TRequestContext>)));
            }

            var interfaceBinder = new InterfaceSyncMiddlewareBinder<TRequestContext>(middleware);
            return app.Use(interfaceBinder.CreateMiddleware);
        }

        var methods = middleware.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        MethodInfo? invokeMethod = null;
        foreach (var method in methods)
        {
            if (string.Equals(method.Name, InvokeMethodName, StringComparison.Ordinal))
            {
                if (invokeMethod is not null)
                {
                    throw new InvalidOperationException(string.Format("Multiple public '{0}' methods are available.", InvokeMethodName));
                }

                invokeMethod = method;
            }
        }

        if (invokeMethod is null)
        {
            throw new InvalidOperationException(string.Format("No public '{0}' method found for middleware of type '{1}'.", InvokeMethodName, middleware));
        }

        if (invokeMethod.ReturnType != typeof(void))
        {
            throw new InvalidOperationException(string.Format("'{0}' cannot have return value.", InvokeMethodName));
        }

        var parameters = invokeMethod.GetParameters();
        if (parameters.Length == 0 || parameters[0].ParameterType != typeof(TRequestContext))
        {
            throw new InvalidOperationException(string.Format("The '{0}' method's first argument must be of type '{1}'.", InvokeMethodName, nameof(TRequestContext)));
        }

        var reflectionBinder = new ReflectionSyncMiddlewareBinder<TRequestContext>(app, middleware, args, invokeMethod, parameters);
        return app.Use(reflectionBinder.CreateMiddleware);
    }
}