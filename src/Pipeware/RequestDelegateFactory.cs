// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: src/Http/Http.Extensions/src/RequestDelegateFactory.cs

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Pipeware.Builder;
using Pipeware.Features;
using Pipeware.Internal;
using Pipeware.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Pipeware;

partial class RequestDelegateFactory<TRequestContext>
{
    private static readonly ParameterExpression HttpContextExpr = ParameterBindingMethodCache<TRequestContext>.SharedExpressions.HttpContextExpr;

    private static readonly MethodCallExpression RouteValuesFeatureExpr = Expression.Call(null, typeof(RequestContextFeatureExtensions).GetMethod(nameof(RequestContextFeatureExtensions.GetRouteValuesFeature))!, HttpContextExpr);

    private static readonly MethodCallExpression QueryFeatureExpr = Expression.Call(null, typeof(RequestContextFeatureExtensions).GetMethod(nameof(RequestContextFeatureExtensions.GetQueryFeature))!, HttpContextExpr);

    private static readonly MethodCallExpression RequestLifetimeFeatureExpr = Expression.Call(null, typeof(RequestContextFeatureExtensions).GetMethod(nameof(RequestContextFeatureExtensions.GetRequestLifetimeFeature))!, HttpContextExpr);

    private static readonly MethodCallExpression ResultFailureFeatureExpr = Expression.Call(null, typeof(RequestContextFeatureExtensions).GetMethod(nameof(RequestContextFeatureExtensions.GetResultFailureFeature))!, HttpContextExpr);

    private static readonly MemberExpression RouteValuesExpr = Expression.Property(RouteValuesFeatureExpr, typeof(IRouteValuesFeature).GetProperty(nameof(IRouteValuesFeature.RouteValues))!);

    private static readonly MemberExpression RequestAbortedExpr = Expression.Property(RequestLifetimeFeatureExpr, typeof(IRequestLifetimeFeature).GetProperty(nameof(IRequestLifetimeFeature.RequestAborted))!);

    private static readonly MemberExpression QueryExpr = Expression.Property(QueryFeatureExpr, typeof(IQueryFeature).GetProperty(nameof(IQueryFeature.Query))!);

    private static readonly MemberExpression IsFailureExpr = Expression.Property(ResultFailureFeatureExpr, typeof(IResultFailureFeature).GetProperty(nameof(IResultFailureFeature.IsFailure))!);

    private static readonly ParameterExpression FilterContextExpr = Expression.Parameter(typeof(EndpointFilterInvocationContext<TRequestContext>), "context");

    private static readonly MemberExpression FilterContextArgumentsExpr = Expression.Property(FilterContextExpr, typeof(EndpointFilterInvocationContext<TRequestContext>).GetProperty(nameof(EndpointFilterInvocationContext<TRequestContext>.Arguments))!);

    private static readonly ParameterExpression InvokedFilterContextExpr = Expression.Parameter(typeof(EndpointFilterInvocationContext<TRequestContext>), "filterContext");

    private static readonly Expression FilterContextRequestContextExpr = Expression.Property(FilterContextExpr, typeof(EndpointFilterInvocationContext<TRequestContext>).GetProperty(nameof(EndpointFilterInvocationContext<TRequestContext>.RequestContext))!);

    private static readonly Expression FilterContextResultFailureFeatureExpr = Expression.Call(null, typeof(RequestContextFeatureExtensions).GetMethod(nameof(RequestContextFeatureExtensions.GetResultFailureFeature))!, FilterContextRequestContextExpr);


    private static readonly Expression FilterContextRequestContextIsFailureExpr = Expression.Property(FilterContextResultFailureFeatureExpr, typeof(IResultFailureFeature).GetProperty(nameof(IResultFailureFeature.IsFailure))!);

    private static readonly MethodInfo ObjectResultWriteResponseOfTAsyncMethod = typeof(RequestDelegateFactory<TRequestContext>).GetMethod(nameof(ExecuteWriteObjectResponseAsyncOfT), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MemberExpression RequestServicesExpr = Expression.Property(HttpContextExpr, typeof(IRequestContext).GetProperty(nameof(IRequestContext.RequestServices))!);

    private static async Task ExecuteWriteObjectResponseAsyncOfT<T>(TRequestContext requestContext, T result)
    {
        var resultFeature = requestContext.GetResultObjectFeature();

        await resultFeature.SetResultAsync(result);
    }

    private static Task ExecuteAwaitedReturn(object? obj, TRequestContext httpContext)
    {
        return ExecuteHandlerHelper.ExecuteReturnAsync(obj, httpContext);
    }

    private static Task ExecuteTaskOfT<T>(Task<T> task, TRequestContext httpContext)
    {
        EnsureRequestTaskNotNull(task);

        static async Task ExecuteAwaited(Task<T> task, TRequestContext httpContext)
        {
            await ExecuteAwaitedReturn(await task, httpContext);
        }

        if (task.IsCompletedSuccessfully)
        {
            return ExecuteAwaitedReturn(task.GetAwaiter().GetResult(), httpContext);
        }

        return ExecuteAwaited(task, httpContext);
    }

    private static Task ExecuteValueTaskOfT<T>(ValueTask<T> task, TRequestContext httpContext)
    {
        static async Task ExecuteAwaited(ValueTask<T> task, TRequestContext httpContext)
        {
            await ExecuteAwaitedReturn(await task, httpContext);
        }

        if (task.IsCompletedSuccessfully)
        {
            return ExecuteAwaitedReturn(task.GetAwaiter().GetResult(), httpContext);
        }

        return ExecuteAwaited(task, httpContext);
    }

    private static Expression CreateEndpointFilterInvocationContextBase(RequestDelegateFactoryContext<TRequestContext> factoryContext, Expression[] arguments)
    {
        // In the event that a constructor matching the arity of the
        // provided parameters is not found, we fall back to using the
        // non-generic implementation of EndpointFilterInvocationContext.
        Expression paramArray = factoryContext.BoxedArgs.Length > 0
            ? Expression.NewArrayInit(typeof(object), factoryContext.BoxedArgs)
            : Expression.Call(ArrayEmptyOfObjectMethod);
        var fallbackConstruction = Expression.New(
            DefaultEndpointFilterInvocationContextConstructor,
            new Expression[] { HttpContextExpr, paramArray });

        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            // For AOT platforms it's not possible to support the closed generic arguments that are based on the
            // parameter arguments dynamically (for value types). In that case, fallback to boxing the argument list.
            return fallbackConstruction;
        }

        var expandedArguments = new Expression[arguments.Length + 1];
        expandedArguments[0] = HttpContextExpr;
        arguments.CopyTo(expandedArguments, 1);

        var constructorType = factoryContext.ArgumentTypes?.Length switch
        {
            1 => typeof(EndpointFilterInvocationContext<,>),
            2 => typeof(EndpointFilterInvocationContext<,,>),
            3 => typeof(EndpointFilterInvocationContext<,,,>),
            4 => typeof(EndpointFilterInvocationContext<,,,,>),
            5 => typeof(EndpointFilterInvocationContext<,,,,,>),
            6 => typeof(EndpointFilterInvocationContext<,,,,,,>),
            7 => typeof(EndpointFilterInvocationContext<,,,,,,,>),
            8 => typeof(EndpointFilterInvocationContext<,,,,,,,,>),
            9 => typeof(EndpointFilterInvocationContext<,,,,,,,,,>),
            10 => typeof(EndpointFilterInvocationContext<,,,,,,,,,,>),
            _ => null
        };

        if (constructorType is not null)
        {
            var constructor = constructorType.MakeGenericType([.. factoryContext.ArgumentTypes!, typeof(TRequestContext)]).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).SingleOrDefault();
            if (constructor == null)
            {
                // new EndpointFilterInvocationContext(httpContext, (object)name_local, (object)int_local);
                return fallbackConstruction;
            }

            // new EndpointFilterInvocationContext<string, int>(httpContext, name_local, int_local);
            return Expression.New(constructor, expandedArguments);
        }

        // new EndpointFilterInvocationContext(httpContext, (object)name_local, (object)int_local);
        return fallbackConstruction;
    }


    private static Func<object?, TRequestContext, Task> HandleRequestBodyAndCompileRequestDelegate(Expression responseWritingMethodCall, RequestDelegateFactoryContext<TRequestContext> factoryContext)
    {
        if (factoryContext.RequestBodyParameter is null)
        {
            if (factoryContext.ParameterBinders.Count > 0)
            {
                // We need to generate the code for reading from the custom binders calling into the delegate
                var continuation = Expression.Lambda<Func<object?, TRequestContext, object?[], Task>>(
                    responseWritingMethodCall, TargetExpr, HttpContextExpr, BoundValuesArrayExpr).Compile();

                // Looping over arrays is faster
                var binders = factoryContext.ParameterBinders.ToArray();
                var count = binders.Length;

                return async (target, httpContext) =>
                {
                    var boundValues = new object?[count];

                    for (var i = 0; i < count; i++)
                    {
                        boundValues[i] = await binders[i](httpContext);
                    }

                    await continuation(target, httpContext, boundValues);
                };
            }

            return Expression.Lambda<Func<object?, TRequestContext, Task>>(
                responseWritingMethodCall, TargetExpr, HttpContextExpr).Compile();
        }

        return HandleRequestBodyAndCompileRequestDelegateWithBody(responseWritingMethodCall, factoryContext);
    }


    private static Func<object?, TRequestContext, Task> HandleRequestBodyAndCompileRequestDelegateWithBody(Expression responseWritingMethodCall, RequestDelegateFactoryContext<TRequestContext> factoryContext)
    {
        Debug.Assert(factoryContext.RequestBodyParameter is not null, "factoryContext.RequestBodyParameter is null for a body.");

        var bodyType = factoryContext.RequestBodyParameter.ParameterType;
        var parameterTypeName = TypeNameHelper.GetTypeDisplayName(factoryContext.RequestBodyParameter.ParameterType, fullName: false);
        var parameterName = factoryContext.RequestBodyParameter.Name;

        Debug.Assert(parameterName is not null, "CreateArgument() should throw if parameter.Name is null.");

        if (factoryContext.ParameterBinders.Count > 0)
        {
            // We need to generate the code for reading from the body before calling into the delegate
            var continuation = Expression.Lambda<Func<object?, TRequestContext, object?, object?[], Task>>(
            responseWritingMethodCall, TargetExpr, HttpContextExpr, BodyValueExpr, BoundValuesArrayExpr).Compile();

            // Looping over arrays is faster
            var binders = factoryContext.ParameterBinders.ToArray();
            var count = binders.Length;

            return async (target, httpContext) =>
            {
                // Run these first so that they can potentially read and rewind the body
                var boundValues = new object?[count];

                for (var i = 0; i < count; i++)
                {
                    boundValues[i] = await binders[i](httpContext);
                }

                var (bodyValue, successful) = await GetBodyAsync(
                    httpContext,
                    bodyType,
                    parameterTypeName,
                    parameterName,
                    factoryContext.AllowEmptyRequestBody,
                    factoryContext.ThrowOnBadRequest);

                if (!successful)
                {
                    return;
                }

                await continuation(target, httpContext, bodyValue, boundValues);
            };
        }
        else
        {
            // We need to generate the code for reading from the body before calling into the delegate
            var continuation = Expression.Lambda<Func<object?, TRequestContext, object?, Task>>(
            responseWritingMethodCall, TargetExpr, HttpContextExpr, BodyValueExpr).Compile();

            return async (target, httpContext) =>
            {
                var (bodyValue, successful) = await GetBodyAsync(
                    httpContext,
                    bodyType,
                    parameterTypeName,
                    parameterName,
                    factoryContext.AllowEmptyRequestBody,
                    factoryContext.ThrowOnBadRequest);

                if (!successful)
                {
                    return;
                }

                await continuation(target, httpContext, bodyValue);
            };
        }

        async Task<(object? BodyValue, bool Successful)> GetBodyAsync(TRequestContext requestContext,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type bodyType,
            string parameterTypeName,
            string parameterType,
            bool allowEmptyRequestBody,
            bool throwOnBadRequest)
        {
            object? defaultBodyValue = null;

            if (allowEmptyRequestBody && bodyType.IsValueType)
            {
                defaultBodyValue = CreateValueType(bodyType);
            }

            var bodyValue = defaultBodyValue;

            if (requestContext.Features.Get<IRequestBodyFeature>() is { } feature)
            {
                try
                {
                    bodyValue = await feature.GetBodyAsync(bodyType);

                    if (bodyValue != null && !bodyType.IsAssignableFrom(bodyValue.GetType()))
                    {
                        Log.InvalidRequestBodyType(requestContext,
                            parameterTypeName,
                            parameterName,
                            bodyValue.GetType().Name,
                            throwOnBadRequest);

                        requestContext.GetResultFailureFeature().IsFailure = true;
                        return (null, false);
                    }
                }
                catch (Exception ex) when (feature.IsBadRequestException(ex, out var preventRethrow))
                {
                    Log.InvalidRequestBody(requestContext,
                        parameterTypeName,
                        parameterName,
                        ex,
                        throwOnBadRequest && !preventRethrow);

                    requestContext.GetResultFailureFeature().IsFailure = true;
                    return (null, false);
                }

                return (bodyValue, true);
            }

            return (bodyValue, true);
        }
    }

    private static void PopulateBuiltInResponseTypeMetadata(Type returnType, EndpointBuilder<TRequestContext> builder)
    {
        if (returnType.IsByRefLike)
        {
            throw GetUnsupportedReturnTypeException(returnType);
        }

        if (returnType == typeof(Task) || returnType == typeof(ValueTask))
        {
            returnType = typeof(void);
        }
        else if (AwaitableInfo.IsTypeAwaitable(returnType, out _))
        {
            var genericTypeDefinition = returnType.IsGenericType ? returnType.GetGenericTypeDefinition() : null;

            if (genericTypeDefinition == typeof(Task<>) || genericTypeDefinition == typeof(ValueTask<>))
            {
                returnType = returnType.GetGenericArguments()[0];
            }
            else
            {
                throw GetUnsupportedReturnTypeException(returnType);
            }
        }

        // Skip void returns and IResults. IResults might implement IEndpointMetadataProvider but otherwise we don't know what it might do.
        if (returnType == typeof(void) || typeof(IResult<TRequestContext>).IsAssignableFrom(returnType))
        {
            return;
        }
    }

    private static Expression CreateArgument(ParameterInfo parameter, RequestDelegateFactoryContext<TRequestContext> factoryContext)
    {
        if (parameter.Name is null)
        {
            throw new InvalidOperationException($"Encountered a parameter of type '{parameter.ParameterType}' without a name. Parameters must have a name.");
        }

        if (parameter.ParameterType.IsByRef)
        {
            var attribute = "ref";

            if (parameter.Attributes.HasFlag(ParameterAttributes.In))
            {
                attribute = "in";
            }
            else if (parameter.Attributes.HasFlag(ParameterAttributes.Out))
            {
                attribute = "out";
            }

            throw new NotSupportedException($"The by reference parameter '{attribute} {TypeNameHelper.GetTypeDisplayName(parameter.ParameterType, fullName: false)} {parameter.Name}' is not supported.");
        }

        var parameterCustomAttributes = parameter.GetCustomAttributes();

        if (parameterCustomAttributes.OfType<IFromRouteMetadata>().FirstOrDefault() is { } routeAttribute)
        {
            var routeName = routeAttribute.Name ?? parameter.Name;
            factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.RouteAttribute);
            if (factoryContext.RouteParameters is { } routeParams && !routeParams.Contains(routeName, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"'{routeName}' is not a route parameter.");
            }

            return BindParameterFromProperty(parameter, RouteValuesExpr, RouteValuesIndexerProperty, routeName, factoryContext, "route");
        }
        else if (parameterCustomAttributes.OfType<IFromQueryMetadata>().FirstOrDefault() is { } queryAttribute)
        {
            factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.QueryAttribute);
            return BindParameterFromProperty(parameter, QueryExpr, QueryIndexerProperty, queryAttribute.Name ?? parameter.Name, factoryContext, "query string");
        }
        else if (parameterCustomAttributes.OfType<IFromBodyMetadata>().FirstOrDefault() is { } bodyAttribute)
        {
            factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.BodyAttribute);
            return BindParameterFromBody(parameter, bodyAttribute.AllowEmpty, factoryContext);
        }
        else if (parameter.CustomAttributes.Any(a => typeof(IFromServiceMetadata).IsAssignableFrom(a.AttributeType)))
        {
            if (parameterCustomAttributes.OfType<FromKeyedServicesAttribute>().FirstOrDefault() is not null)
            {
                throw new NotSupportedException(
                    $"The {nameof(FromKeyedServicesAttribute)} is not supported on parameters that are also annotated with {nameof(IFromServiceMetadata)}.");
            }
            factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.ServiceAttribute);
            return BindParameterFromService(parameter, factoryContext);
        }
        else if (parameterCustomAttributes.OfType<FromKeyedServicesAttribute>().FirstOrDefault() is { } keyedServicesAttribute)
        {
            var key = keyedServicesAttribute.Key;
            return BindParameterFromKeyedService(parameter, key, factoryContext);
        }
        else if (parameterCustomAttributes.OfType<AsParametersAttribute>().Any())
        {
            if (parameter is PropertyAsParameterInfo<TRequestContext>)
            {
                throw new NotSupportedException(
                    $"Nested {nameof(AsParametersAttribute)} is not supported and should be used only for handler parameters.");
            }

            return BindParameterFromProperties(parameter, factoryContext);
        }
        else if (parameter.ParameterType == typeof(TRequestContext))
        {
            return HttpContextExpr;
        }
        else if (parameter.ParameterType == typeof(CancellationToken))
        {
            return RequestAbortedExpr;
        }
        else if (ParameterBindingMethodCache.HasBindAsyncMethod(parameter))
        {
            return BindParameterFromBindAsync(parameter, factoryContext);
        }
        else if (parameter.ParameterType == typeof(string) || ParameterBindingMethodCache.HasTryParseMethod(parameter.ParameterType))
        {
            // 1. We bind from route values only, if route parameters are non-null and the parameter name is in that set.
            // 2. We bind from query only, if route parameters are non-null and the parameter name is NOT in that set.
            // 3. Otherwise, we fallback to route or query if route parameters is null (it means we don't know what route parameters are defined). This case only happens
            // when RDF.Create is manually invoked.
            if (factoryContext.RouteParameters is { } routeParams)
            {
                if (routeParams.Contains(parameter.Name, StringComparer.OrdinalIgnoreCase))
                {
                    // We're in the fallback case and we have a parameter and route parameter match so don't fallback
                    // to query string in this case
                    factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.RouteParameter);
                    return BindParameterFromProperty(parameter, RouteValuesExpr, RouteValuesIndexerProperty, parameter.Name, factoryContext, "route");
                }
                else
                {
                    factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.QueryStringParameter);
                    return BindParameterFromProperty(parameter, QueryExpr, QueryIndexerProperty, parameter.Name, factoryContext, "query string");
                }
            }

            factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.RouteOrQueryStringParameter);
            return BindParameterFromRouteValueOrQueryString(parameter, parameter.Name, factoryContext);
        }
        else
        {
            if (factoryContext.ServiceProviderIsService is IServiceProviderIsService serviceProviderIsService)
            {
                if (serviceProviderIsService.IsService(parameter.ParameterType))
                {
                    factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.ServiceParameter);
                    return Expression.Call(GetRequiredServiceMethod.MakeGenericMethod(parameter.ParameterType), RequestServicesExpr);
                }
            }

            factoryContext.HasInferredBody = true;
            factoryContext.TrackedParameters.Add(parameter.Name, RequestDelegateFactoryConstants.BodyParameter);
            return BindParameterFromBody(parameter, allowEmpty: false, factoryContext);
        }
    }

    private static Expression BindParameterFromBody(ParameterInfo parameter, bool allowEmpty, RequestDelegateFactoryContext<TRequestContext> factoryContext)
    {
        if (factoryContext.RequestBodyParameter is not null)
        {
            factoryContext.HasMultipleBodyParameters = true;
            var parameterName = parameter.Name;

            Debug.Assert(parameterName is not null, "CreateArgument() should throw if parameter.Name is null.");

            if (factoryContext.TrackedParameters.ContainsKey(parameterName))
            {
                factoryContext.TrackedParameters.Remove(parameterName);
                factoryContext.TrackedParameters.Add(parameterName, "UNKNOWN");
            }
        }

        var isOptional = IsOptionalParameter(parameter, factoryContext);

        factoryContext.RequestBodyParameter = parameter;
        factoryContext.AllowEmptyRequestBody = allowEmpty || isOptional;

        if (!factoryContext.AllowEmptyRequestBody)
        {
            if (factoryContext.HasInferredBody)
            {
                // if (bodyValue == null)
                // {
                //    wasParamCheckFailure = true;
                //    Log.ImplicitBodyNotProvided(httpContext, "todo", ThrowOnBadRequest);
                // }
                factoryContext.ParamCheckExpressions.Add(Expression.Block(
                    Expression.IfThen(
                        Expression.Equal(BodyValueExpr, Expression.Constant(null)),
                        Expression.Block(
                            Expression.Assign(WasParamCheckFailureExpr, Expression.Constant(true)),
                            Expression.Call(LogImplicitBodyNotProvidedMethod,
                                HttpContextExpr,
                                Expression.Constant(parameter.Name),
                                Expression.Constant(factoryContext.ThrowOnBadRequest)
                            )
                        )
                    )
                ));
            }
            else
            {
                // If the parameter is required or the user has not explicitly
                // set allowBody to be empty then validate that it is required.
                //
                // if (bodyValue == null)
                // {
                //      wasParamCheckFailure = true;
                //      Log.RequiredParameterNotProvided(httpContext, "Todo", "todo", "body", ThrowOnBadRequest);
                // }
                var checkRequiredBodyBlock = Expression.Block(
                    Expression.IfThen(
                    Expression.Equal(BodyValueExpr, Expression.Constant(null)),
                        Expression.Block(
                            Expression.Assign(WasParamCheckFailureExpr, Expression.Constant(true)),
                            Expression.Call(LogRequiredParameterNotProvidedMethod,
                                HttpContextExpr,
                                Expression.Constant(TypeNameHelper.GetTypeDisplayName(parameter.ParameterType, fullName: false)),
                                Expression.Constant(parameter.Name),
                                Expression.Constant("body"),
                                Expression.Constant(factoryContext.ThrowOnBadRequest))
                        )
                    )
                );
                factoryContext.ParamCheckExpressions.Add(checkRequiredBodyBlock);
            }
        }
        else if (parameter.HasDefaultValue)
        {
            // Convert(bodyValue ?? SomeDefault, Todo)
            return Expression.Convert(
                Expression.Coalesce(BodyValueExpr, Expression.Constant(parameter.DefaultValue)),
                parameter.ParameterType);
        }

        // Convert(bodyValue, Todo)
        return Expression.Convert(BodyValueExpr, parameter.ParameterType);
    }


    static partial class Log
    {
        public static void InvalidRequestBodyType(TRequestContext httpContext, string parameterName, string parameterTypeName, string bodyTypeName, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, RequestDelegateCreationLogging.InvalidRequestBodyTypeExceptionMessage, parameterName, parameterTypeName, bodyTypeName);
                throw new BadRequestException(message);
            }

            InvalidRequestBodyType(GetLogger(httpContext), parameterName, parameterTypeName, bodyTypeName);
        }

        [LoggerMessage(RequestDelegateCreationLogging.InvalidRequestBodyTypeEventId, LogLevel.Debug, RequestDelegateCreationLogging.InvalidRequestBodyTypeLogMessage, EventName = RequestDelegateCreationLogging.InvalidRequestBodyTypeEventName)]
        private static partial void InvalidRequestBodyType(ILogger logger, string parameterName, string parameterType, string bodyType);

        public static void InvalidRequestBody(TRequestContext httpContext, string parameterName, string parameterTypeName, Exception exception, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, RequestDelegateCreationLogging.InvalidRequestBodyExceptionMessage, parameterName, parameterTypeName);
                throw new BadRequestException(message, exception);
            }

            InvalidRequestBody(GetLogger(httpContext), parameterName, parameterTypeName, exception);
        }

        [LoggerMessage(RequestDelegateCreationLogging.InvalidRequestBodyEventId, LogLevel.Debug, RequestDelegateCreationLogging.InvalidRequestBodyLogMessage, EventName = RequestDelegateCreationLogging.InvalidRequestBodyEventName)]
        private static partial void InvalidRequestBody(ILogger logger, string parameterName, string parameterType, Exception exception);

    }
}
