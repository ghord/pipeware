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

    private sealed class ReflectionSyncMiddlewareBinder<TRequestContext> where TRequestContext : class, IRequestContext
    {
        private readonly ISyncPipelineBuilder<TRequestContext> _app;
        [DynamicallyAccessedMembers(MiddlewareAccessibility)]
        private readonly Type _middleware;
        private readonly object?[] _args;
        private readonly MethodInfo _invokeMethod;
        private readonly ParameterInfo[] _parameters;

        public ReflectionSyncMiddlewareBinder(
            ISyncPipelineBuilder<TRequestContext> app,
            [DynamicallyAccessedMembers(MiddlewareAccessibility)] Type middleware,
            object?[] args,
            MethodInfo invokeMethod,
            ParameterInfo[] parameters)
        {
            _app = app;
            _middleware = middleware;
            _args = args;
            _invokeMethod = invokeMethod;
            _parameters = parameters;
        }

        // The CreateMiddleware method name is used by ApplicationBuilder to resolve the middleware type.
        public SyncRequestDelegate<TRequestContext> CreateMiddleware(SyncRequestDelegate<TRequestContext> next)
        {
            var ctorArgs = new object[_args.Length + 1];
            ctorArgs[0] = next;
            Array.Copy(_args, 0, ctorArgs, 1, _args.Length);
            var instance = ActivatorUtilities.CreateInstance(_app.ApplicationServices, _middleware, ctorArgs);
            if (_parameters.Length == 1)
            {
                return (SyncRequestDelegate<TRequestContext>)_invokeMethod.CreateDelegate(typeof(SyncRequestDelegate<TRequestContext>), instance);
            }

            // Performance optimization: Use compiled expressions to invoke middleware with services injected in Invoke.
            // If IsDynamicCodeCompiled is false then use standard reflection to avoid overhead of interpreting expressions.
            var factory = RuntimeFeature.IsDynamicCodeCompiled
                ? CompileSyncExpression<object, TRequestContext>(_invokeMethod, _parameters)
                : SyncReflectionFallback<object, TRequestContext>(_invokeMethod, _parameters);

            return context =>
            {
                var serviceProvider = context.RequestServices ?? _app.ApplicationServices;
                if (serviceProvider == null)
                {
                    throw new InvalidOperationException(string.Format("'{0}' is not available.", nameof(IServiceProvider)));
                }

                factory(instance, context, serviceProvider);
            };
        }

        public override string ToString() => _middleware.ToString();
    }

    private sealed class InterfaceSyncMiddlewareBinder<TRequestContext> where TRequestContext : class, IRequestContext
    {
        private readonly Type _middlewareType;

        public InterfaceSyncMiddlewareBinder(Type middlewareType)
        {
            _middlewareType = middlewareType;
        }

        // The CreateMiddleware method name is used by ApplicationBuilder to resolve the middleware type.
        public SyncRequestDelegate<TRequestContext> CreateMiddleware(SyncRequestDelegate<TRequestContext> next)
        {
            return context =>
            {
                var middlewareFactory = (ISyncMiddlewareFactory<TRequestContext>?)context.RequestServices.GetService(typeof(ISyncMiddlewareFactory<TRequestContext>));
                if (middlewareFactory == null)
                {
                    // No middleware factory
                    throw new InvalidOperationException(string.Format("No service for type '{0}' has been registered.", typeof(ISyncMiddlewareFactory<TRequestContext>)));
                }

                var middleware = middlewareFactory.Create(_middlewareType);
                if (middleware == null)
                {
                    // The factory returned null, it's a broken implementation
                    throw new InvalidOperationException(string.Format("'{0}' failed to create middleware of type '{1}'.", middlewareFactory.GetType(), _middlewareType));
                }

                try
                {
                    middleware.Invoke(context, next);
                }
                finally
                {
                    middlewareFactory.Release(middleware);
                }
            };
        }

        public override string ToString() => _middlewareType.ToString();
    }

    private static Action<T, TRequestContext, IServiceProvider> SyncReflectionFallback<T, TRequestContext>(MethodInfo methodInfo, ParameterInfo[] parameters) where TRequestContext : class, IRequestContext
    {
        Debug.Assert(!RuntimeFeature.IsDynamicCodeSupported, "Use reflection fallback when dynamic code is not supported.");

        for (var i = 1; i < parameters.Length; i++)
        {
            var parameterType = parameters[i].ParameterType;
            if (parameterType.IsByRef)
            {
                throw new NotSupportedException(string.Format("The '{0}' method must not have ref or out parameters.", InvokeMethodName));
            }
        }

        return (middleware, context, serviceProvider) =>
        {
            var methodArguments = new object[parameters.Length];
            methodArguments[0] = context;
            for (var i = 1; i < parameters.Length; i++)
            {
                methodArguments[i] = GetService(serviceProvider, parameters[i].ParameterType, methodInfo.DeclaringType!);
            }

            methodInfo.Invoke(middleware, BindingFlags.DoNotWrapExceptions, binder: null, methodArguments, culture: null);
        };
    }

    private static Action<T, TRequestContext, IServiceProvider> CompileSyncExpression<T, TRequestContext>(MethodInfo methodInfo, ParameterInfo[] parameters) where TRequestContext : class, IRequestContext
    {
        Debug.Assert(RuntimeFeature.IsDynamicCodeSupported, "Use compiled expression when dynamic code is supported.");

        // If we call something like
        //
        // public class Middleware
        // {
        //    public Task Invoke(HttpContext context, ILoggerFactory loggerFactory)
        //    {
        //
        //    }
        // }
        //

        // We'll end up with something like this:
        //   Generic version:
        //
        //   Task Invoke(Middleware instance, HttpContext httpContext, IServiceProvider provider)
        //   {
        //      return instance.Invoke(httpContext, (ILoggerFactory)UseMiddlewareExtensions.GetService(provider, typeof(ILoggerFactory));
        //   }

        //   Non generic version:
        //
        //   Task Invoke(object instance, HttpContext httpContext, IServiceProvider provider)
        //   {
        //      return ((Middleware)instance).Invoke(httpContext, (ILoggerFactory)UseMiddlewareExtensions.GetService(provider, typeof(ILoggerFactory));
        //   }

        var middleware = typeof(T);

        var httpContextArg = Expression.Parameter(typeof(TRequestContext), "httpContext");
        var providerArg = Expression.Parameter(typeof(IServiceProvider), "serviceProvider");
        var instanceArg = Expression.Parameter(middleware, "middleware");

        var methodArguments = new Expression[parameters.Length];
        methodArguments[0] = httpContextArg;
        for (var i = 1; i < parameters.Length; i++)
        {
            var parameterType = parameters[i].ParameterType;
            if (parameterType.IsByRef)
            {
                throw new NotSupportedException(string.Format("The '{0}' method must not have ref or out parameters.", InvokeMethodName));
            }

            var parameterTypeExpression = new Expression[]
            {
                providerArg,
                Expression.Constant(parameterType, typeof(Type)),
                Expression.Constant(methodInfo.DeclaringType, typeof(Type))
            };

            var getServiceCall = Expression.Call(GetServiceInfo, parameterTypeExpression);
            methodArguments[i] = Expression.Convert(getServiceCall, parameterType);
        }

        Expression middlewareInstanceArg = instanceArg;
        if (methodInfo.DeclaringType != null && methodInfo.DeclaringType != typeof(T))
        {
            middlewareInstanceArg = Expression.Convert(middlewareInstanceArg, methodInfo.DeclaringType);
        }

        var body = Expression.Call(middlewareInstanceArg, methodInfo, methodArguments);

        var lambda = Expression.Lambda<Action<T, TRequestContext, IServiceProvider>>(body, instanceArg, httpContextArg, providerArg);

        return lambda.Compile();
    }
}