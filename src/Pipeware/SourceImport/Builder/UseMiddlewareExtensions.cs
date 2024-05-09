// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/Extensions/UseMiddlewareExtensions.cs
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
    internal const string InvokeMethodName = "Invoke";
    internal const string InvokeAsyncMethodName = "InvokeAsync";

    private static readonly MethodInfo GetServiceInfo = typeof(UseMiddlewareExtensions).GetMethod(nameof(GetService), BindingFlags.NonPublic | BindingFlags.Static)!;

    // We're going to keep all public constructors and public methods on middleware
    private const DynamicallyAccessedMemberTypes MiddlewareAccessibility =
        DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods;

    /// <summary>
    /// Adds a middleware type to the application's request pipeline.
    /// </summary>
    /// <param name="app">The <see cref="IPipelineBuilder{TRequestContext}"/> instance.</param>
    /// <param name="middleware">The middleware type.</param>
    /// <param name="args">The arguments to pass to the middleware type instance's constructor.</param>
    /// <returns>The <see cref="IPipelineBuilder{TRequestContext}"/> instance.</returns>
    public static IPipelineBuilder<TRequestContext> UseMiddleware<TRequestContext>(
        this IPipelineBuilder<TRequestContext> app,
        [DynamicallyAccessedMembers(MiddlewareAccessibility)] Type middleware,
        params object?[] args) where TRequestContext : class, IRequestContext
    {
        if (typeof(IMiddleware<TRequestContext>).IsAssignableFrom(middleware))
        {
            // IMiddleware doesn't support passing args directly since it's
            // activated from the container
            if (args.Length > 0)
            {
                throw new NotSupportedException(string.Format("Types that implement '{0}' do not support explicit arguments.", typeof(IMiddleware<TRequestContext>)));
            }

            var interfaceBinder = new InterfaceMiddlewareBinder<TRequestContext>(middleware);
            return app.Use(interfaceBinder.CreateMiddleware);
        }

        var methods = middleware.GetMethods(BindingFlags.Instance | BindingFlags.Public);
        MethodInfo? invokeMethod = null;
        foreach (var method in methods)
        {
            if (string.Equals(method.Name, InvokeMethodName, StringComparison.Ordinal) || string.Equals(method.Name, InvokeAsyncMethodName, StringComparison.Ordinal))
            {
                if (invokeMethod is not null)
                {
                    throw new InvalidOperationException(string.Format("Multiple public '{0}' or '{1}' methods are available.", InvokeMethodName, InvokeAsyncMethodName));
                }

                invokeMethod = method;
            }
        }

        if (invokeMethod is null)
        {
            throw new InvalidOperationException(string.Format("No public '{0}' or '{1}' method found for middleware of type '{2}'.", InvokeMethodName, InvokeAsyncMethodName, middleware));
        }

        if (!typeof(Task).IsAssignableFrom(invokeMethod.ReturnType))
        {
            throw new InvalidOperationException(string.Format("'{0}' or '{1}' does not return an object of type '{2}'.", InvokeMethodName, InvokeAsyncMethodName, nameof(Task)));
        }

        var parameters = invokeMethod.GetParameters();
        if (parameters.Length == 0 || parameters[0].ParameterType != typeof(TRequestContext))
        {
            throw new InvalidOperationException(string.Format("The '{0}' or '{1}' method's first argument must be of type '{2}'.", InvokeMethodName, InvokeAsyncMethodName, nameof(TRequestContext)));
        }

        var reflectionBinder = new ReflectionMiddlewareBinder<TRequestContext>(app, middleware, args, invokeMethod, parameters);
        return app.Use(reflectionBinder.CreateMiddleware);
    }

    private sealed class ReflectionMiddlewareBinder<TRequestContext> where TRequestContext : class, IRequestContext
    {
        private readonly IPipelineBuilder<TRequestContext> _app;
        [DynamicallyAccessedMembers(MiddlewareAccessibility)]
        private readonly Type _middleware;
        private readonly object?[] _args;
        private readonly MethodInfo _invokeMethod;
        private readonly ParameterInfo[] _parameters;

        public ReflectionMiddlewareBinder(
            IPipelineBuilder<TRequestContext> app,
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
        public RequestDelegate<TRequestContext> CreateMiddleware(RequestDelegate<TRequestContext> next)
        {
            var ctorArgs = new object[_args.Length + 1];
            ctorArgs[0] = next;
            Array.Copy(_args, 0, ctorArgs, 1, _args.Length);
            var instance = ActivatorUtilities.CreateInstance(_app.ApplicationServices, _middleware, ctorArgs);
            if (_parameters.Length == 1)
            {
                return (RequestDelegate<TRequestContext>)_invokeMethod.CreateDelegate(typeof(RequestDelegate<TRequestContext>), instance);
            }

            // Performance optimization: Use compiled expressions to invoke middleware with services injected in Invoke.
            // If IsDynamicCodeCompiled is false then use standard reflection to avoid overhead of interpreting expressions.
            var factory = RuntimeFeature.IsDynamicCodeCompiled
                ? CompileExpression<object, TRequestContext>(_invokeMethod, _parameters)
                : ReflectionFallback<object, TRequestContext>(_invokeMethod, _parameters);

            return context =>
            {
                var serviceProvider = context.RequestServices ?? _app.ApplicationServices;
                if (serviceProvider == null)
                {
                    throw new InvalidOperationException(string.Format("'{0}' is not available.", nameof(IServiceProvider)));
                }

                return factory(instance, context, serviceProvider);
            };
        }

        public override string ToString() => _middleware.ToString();
    }

    private sealed class InterfaceMiddlewareBinder<TRequestContext> where TRequestContext : class, IRequestContext
    {
        private readonly Type _middlewareType;

        public InterfaceMiddlewareBinder(Type middlewareType)
        {
            _middlewareType = middlewareType;
        }

        // The CreateMiddleware method name is used by ApplicationBuilder to resolve the middleware type.
        public RequestDelegate<TRequestContext> CreateMiddleware(RequestDelegate<TRequestContext> next)
        {
            return async context =>
            {
                var middlewareFactory = (IMiddlewareFactory<TRequestContext>?)context.RequestServices.GetService(typeof(IMiddlewareFactory<TRequestContext>));
                if (middlewareFactory == null)
                {
                    // No middleware factory
                    throw new InvalidOperationException(string.Format("No service for type '{0}' has been registered.", typeof(IMiddlewareFactory<TRequestContext>)));
                }

                var middleware = middlewareFactory.Create(_middlewareType);
                if (middleware == null)
                {
                    // The factory returned null, it's a broken implementation
                    throw new InvalidOperationException(string.Format("'{0}' failed to create middleware of type '{1}'.", middlewareFactory.GetType(), _middlewareType));
                }

                try
                {
                    await middleware.InvokeAsync(context, next);
                }
                finally
                {
                    middlewareFactory.Release(middleware);
                }
            };
        }

        public override string ToString() => _middlewareType.ToString();
    }

    private static Func<T, TRequestContext, IServiceProvider, Task> ReflectionFallback<T, TRequestContext>(MethodInfo methodInfo, ParameterInfo[] parameters) where TRequestContext : class, IRequestContext
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

            return (Task)methodInfo.Invoke(middleware, BindingFlags.DoNotWrapExceptions, binder: null, methodArguments, culture: null)!;
        };
    }

    private static Func<T, TRequestContext, IServiceProvider, Task> CompileExpression<T, TRequestContext>(MethodInfo methodInfo, ParameterInfo[] parameters) where TRequestContext : class, IRequestContext
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

        var lambda = Expression.Lambda<Func<T, TRequestContext, IServiceProvider, Task>>(body, instanceArg, httpContextArg, providerArg);

        return lambda.Compile();
    }

    private static object GetService(IServiceProvider sp, Type type, Type middleware)
    {
        var service = sp.GetService(type);
        if (service == null)
        {
            throw new InvalidOperationException(string.Format("Unable to resolve service for type '{0}' while attempting to Invoke middleware '{1}'.", type, middleware));
        }

        return service;
    }
}