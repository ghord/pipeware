// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Extensions/src/RequestDelegateFactory.cs
// Source Sha256: 82a882f7025ace04197d78e9987874459145c700

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using Pipeware.Builder;

using Pipeware.Features;
using Pipeware.Results;

using Pipeware.Metadata;

using Pipeware.Routing;
using Microsoft.Extensions.DependencyInjection;
using Pipeware.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Pipeware;

/// <summary>
/// Creates <see cref="RequestDelegate{TRequestContext}"/> implementations from <see cref="Delegate"/> request handlers.
/// </summary>
[RequiresUnreferencedCode("RequestDelegateFactory performs object creation, serialization and deserialization on the delegates and its parameters. This cannot be statically analyzed.")]
[RequiresDynamicCode("RequestDelegateFactory performs object creation, serialization and deserialization on the delegates and its parameters. This cannot be statically analyzed.")]
public static partial class RequestDelegateFactory<TRequestContext> where TRequestContext : class, IRequestContext
{
    private static readonly ParameterBindingMethodCache<TRequestContext> ParameterBindingMethodCache = new();

    private static readonly MethodInfo ExecuteTaskWithEmptyResultMethod = typeof(RequestDelegateFactory<TRequestContext>).GetMethod(nameof(ExecuteTaskWithEmptyResult), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteValueTaskWithEmptyResultMethod = typeof(RequestDelegateFactory<TRequestContext>).GetMethod(nameof(ExecuteValueTaskWithEmptyResult), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteTaskOfTMethod = typeof(RequestDelegateFactory<TRequestContext>).GetMethod(nameof(ExecuteTaskOfT), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteTaskOfObjectMethod = typeof(RequestDelegateFactory<TRequestContext>).GetMethod(nameof(ExecuteTaskOfObject), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteValueTaskOfObjectMethod = typeof(RequestDelegateFactory<TRequestContext>).GetMethod(nameof(ExecuteValueTaskOfObject), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteValueTaskOfTMethod = typeof(RequestDelegateFactory<TRequestContext>).GetMethod(nameof(ExecuteValueTaskOfT), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteValueTaskMethod = typeof(RequestDelegateFactory<TRequestContext>).GetMethod(nameof(ExecuteValueTask), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteTaskResultOfTMethod = typeof(RequestDelegateFactory<TRequestContext>).GetMethod(nameof(ExecuteTaskResult), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteValueResultTaskOfTMethod = typeof(RequestDelegateFactory<TRequestContext>).GetMethod(nameof(ExecuteValueTaskResult), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ExecuteAwaitedReturnMethod = typeof(RequestDelegateFactory<TRequestContext>).GetMethod(nameof(ExecuteAwaitedReturn), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo GetRequiredServiceMethod = typeof(ServiceProviderServiceExtensions).GetMethod(nameof(ServiceProviderServiceExtensions.GetRequiredService), BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(IServiceProvider) })!;
    private static readonly MethodInfo GetServiceMethod = typeof(ServiceProviderServiceExtensions).GetMethod(nameof(ServiceProviderServiceExtensions.GetService), BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(IServiceProvider) })!;
    private static readonly MethodInfo GetRequiredKeyedServiceMethod = typeof(ServiceProviderKeyedServiceExtensions).GetMethod(nameof(ServiceProviderKeyedServiceExtensions.GetRequiredKeyedService), BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(IServiceProvider), typeof(object) })!;
    private static readonly MethodInfo GetKeyedServiceMethod = typeof(ServiceProviderKeyedServiceExtensions).GetMethod(nameof(ServiceProviderKeyedServiceExtensions.GetKeyedService), BindingFlags.Public | BindingFlags.Static, new Type[] { typeof(IServiceProvider), typeof(object) })!;
    private static readonly MethodInfo ResultWriteResponseAsyncMethod = typeof(RequestDelegateFactory<TRequestContext>).GetMethod(nameof(ExecuteResultWriteResponse), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo StringIsNullOrEmptyMethod = typeof(string).GetMethod(nameof(string.IsNullOrEmpty), BindingFlags.Static | BindingFlags.Public)!;
    private static readonly MethodInfo WrapObjectAsValueTaskMethod = typeof(RequestDelegateFactory<TRequestContext>).GetMethod(nameof(WrapObjectAsValueTask), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo TaskOfTToValueTaskOfObjectMethod = typeof(RequestDelegateFactory<TRequestContext>).GetMethod(nameof(TaskOfTToValueTaskOfObject), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ValueTaskOfTToValueTaskOfObjectMethod = typeof(RequestDelegateFactory<TRequestContext>).GetMethod(nameof(ValueTaskOfTToValueTaskOfObject), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ArrayEmptyOfObjectMethod = typeof(Array).GetMethod(nameof(Array.Empty), BindingFlags.Public | BindingFlags.Static)!.MakeGenericMethod(new Type[] { typeof(object) });

    private static readonly PropertyInfo QueryIndexerProperty = typeof(IQueryCollection).GetProperty("Item")!;
    private static readonly PropertyInfo RouteValuesIndexerProperty = typeof(RouteValueDictionary).GetProperty("Item")!;

    private static readonly MethodInfo LogParameterBindingFailedMethod = GetMethodInfo<Action<TRequestContext, string, string, string, bool>>((requestContext, parameterType, parameterName, sourceValue, shouldThrow) =>
        Log.ParameterBindingFailed(requestContext, parameterType, parameterName, sourceValue, shouldThrow));
    private static readonly MethodInfo LogRequiredParameterNotProvidedMethod = GetMethodInfo<Action<TRequestContext, string, string, string, bool>>((requestContext, parameterType, parameterName, source, shouldThrow) =>
        Log.RequiredParameterNotProvided(requestContext, parameterType, parameterName, source, shouldThrow));
    private static readonly MethodInfo LogImplicitBodyNotProvidedMethod = GetMethodInfo<Action<TRequestContext, string, bool>>((requestContext, parameterName, shouldThrow) =>
        Log.ImplicitBodyNotProvided(requestContext, parameterName, shouldThrow));

    private static readonly ParameterExpression TargetExpr = Expression.Parameter(typeof(object), "target");
    private static readonly ParameterExpression BodyValueExpr = Expression.Parameter(typeof(object), "bodyValue");
    private static readonly ParameterExpression WasParamCheckFailureExpr = Expression.Variable(typeof(bool), "wasParamCheckFailure");
    private static readonly ParameterExpression BoundValuesArrayExpr = Expression.Parameter(typeof(object[]), "boundValues");
    private static readonly MemberExpression CompletedTaskExpr = Expression.Property(null, (PropertyInfo)GetMemberInfo<Func<Task>>(() => Task.CompletedTask));
    private static readonly NewExpression EmptyHttpResultValueTaskExpr = Expression.New(typeof(ValueTask<object>).GetConstructor(new[] { typeof(EmptyResult<TRequestContext>) })!, Expression.Property(null, typeof(EmptyResult<TRequestContext>), nameof(EmptyResult<TRequestContext>.Instance)));
    private static readonly ParameterExpression TempSourceStringExpr = ParameterBindingMethodCache<TRequestContext>.SharedExpressions.TempSourceStringExpr;
    private static readonly BinaryExpression TempSourceStringNotNullExpr = Expression.NotEqual(TempSourceStringExpr, Expression.Constant(null));
    private static readonly BinaryExpression TempSourceStringNullExpr = Expression.Equal(TempSourceStringExpr, Expression.Constant(null));
    private static readonly UnaryExpression TempSourceStringIsNotNullOrEmptyExpr = Expression.Not(Expression.Call(StringIsNullOrEmptyMethod, TempSourceStringExpr));

    private static readonly ConstructorInfo DefaultEndpointFilterInvocationContextConstructor = typeof(DefaultEndpointFilterInvocationContext<TRequestContext>).GetConstructor(new[] { typeof(TRequestContext), typeof(object[]) })!;
    private static readonly MethodInfo EndpointFilterInvocationContextGetArgument = typeof(EndpointFilterInvocationContext<TRequestContext>).GetMethod(nameof(EndpointFilterInvocationContext<TRequestContext>.GetArgument))!;
    private static readonly PropertyInfo ListIndexer = typeof(IList<object>).GetProperty("Item")!;
    private static readonly MethodInfo AsMemoryMethod = new Func<char[]?, int, int, Memory<char>>(MemoryExtensions.AsMemory).Method;
    private static readonly MethodInfo ArrayPoolSharedReturnMethod = typeof(ArrayPool<char>).GetMethod(nameof(ArrayPool<char>.Shared.Return))!;

    /// <summary>
    /// Returns metadata inferred automatically for the <see cref="RequestDelegate{TRequestContext}"/> created by <see cref="Create(Delegate, RequestDelegateFactoryOptions<TRequestContext>?, RequestDelegateMetadataResult?)"/>.
    /// This includes metadata inferred by <see cref="IEndpointMetadataProvider{TRequestContext}"/> and <see cref="IEndpointParameterMetadataProvider{TRequestContext}"/> implemented by parameter and return types to the <paramref name="methodInfo"/>.
    /// </summary>
    /// <param name="methodInfo">The <see cref="MethodInfo"/> for the route handler to be passed to <see cref="Create(Delegate, RequestDelegateFactoryOptions<TRequestContext>?, RequestDelegateMetadataResult?)"/>.</param>
    /// <param name="options">The options that will be used when calling <see cref="Create(Delegate, RequestDelegateFactoryOptions<TRequestContext>?, RequestDelegateMetadataResult?)"/>.</param>
    /// <returns>The <see cref="RequestDelegateMetadataResult"/> to be passed to <see cref="Create(Delegate, RequestDelegateFactoryOptions<TRequestContext>?, RequestDelegateMetadataResult?)"/>.</returns>
    public static RequestDelegateMetadataResult InferMetadata(MethodInfo methodInfo, RequestDelegateFactoryOptions<TRequestContext>? options = null)
    {
        var factoryContext = CreateFactoryContext(options);
        factoryContext.ArgumentExpressions = CreateArgumentsAndInferMetadata(methodInfo, factoryContext);

        return new RequestDelegateMetadataResult
        {
            EndpointMetadata = AsReadOnlyList(factoryContext.EndpointBuilder.Metadata),
            CachedFactoryContext = factoryContext,
        };
    }

    /// <summary>
    /// Creates a <see cref="RequestDelegate{TRequestContext}"/> implementation for <paramref name="handler"/>.
    /// </summary>
    /// <param name="handler">A request handler with any number of custom parameters that often produces a response with its return value.</param>
    /// <param name="options">The <see cref="RequestDelegateFactoryOptions{TRequestContext}"/> used to configure the behavior of the handler.</param>
    /// <returns>The <see cref="RequestDelegateResult{TRequestContext}"/>.</returns>
    public static RequestDelegateResult<TRequestContext> Create(Delegate handler, RequestDelegateFactoryOptions<TRequestContext>? options)
    {
        return Create(handler, options, metadataResult: null);
    }

    /// <summary>
    /// Creates a <see cref="RequestDelegate{TRequestContext}"/> implementation for <paramref name="handler"/>.
    /// </summary>
    /// <param name="handler">A request handler with any number of custom parameters that often produces a response with its return value.</param>
    /// <param name="options">The <see cref="RequestDelegateFactoryOptions{TRequestContext}"/> used to configure the behavior of the handler.</param>
    /// <param name="metadataResult">
    /// The result returned from <see cref="InferMetadata(MethodInfo, RequestDelegateFactoryOptions<TRequestContext>?)"/> if that was used to inferring metadata before creating the final RequestDelegate.
    /// If <see langword="null"/>, this call to <see cref="Create(Delegate, RequestDelegateFactoryOptions<TRequestContext>?, RequestDelegateMetadataResult?)"/> method will infer the metadata that
    /// <see cref="InferMetadata(MethodInfo, RequestDelegateFactoryOptions<TRequestContext>?)"/> would have inferred for the same <see cref="Delegate.Method"/> and populate <see cref="RequestDelegateFactoryOptions.EndpointBuilder{TRequestContext}"/>
    /// with that metadata. Otherwise, this metadata inference will be skipped as this step has already been done.
    /// </param>
    /// <returns>The <see cref="RequestDelegateResult{TRequestContext}"/>.</returns>
    [SuppressMessage("ApiDesign", "RS0027:Public API with optional parameter(s) should have the most parameters amongst its public overloads.", Justification = "Required to maintain compatibility")]
    public static RequestDelegateResult<TRequestContext> Create(Delegate handler, RequestDelegateFactoryOptions<TRequestContext>? options = null, RequestDelegateMetadataResult? metadataResult = null)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var targetExpression = handler.Target switch
        {
            object => Expression.Convert(TargetExpr, handler.Target.GetType()),
            null => null,
        };

        var factoryContext = CreateFactoryContext(options, metadataResult, handler);

        Expression<Func<TRequestContext, object?>> targetFactory = (requestContext) => handler.Target;
        var targetableRequestDelegate = CreateTargetableRequestDelegate(handler.Method, targetExpression, factoryContext, targetFactory);

        RequestDelegate<TRequestContext> finalRequestDelegate = targetableRequestDelegate switch
        {
            // handler is a RequestDelegate that has not been modified by a filter. Short-circuit and return the original RequestDelegate back.
            // It's possible a filter factory has still modified the endpoint metadata though.
            null => (RequestDelegate<TRequestContext>)handler,
            _ => requestContext=> targetableRequestDelegate(handler.Target, requestContext),
        };

        return CreateRequestDelegateResult(finalRequestDelegate, factoryContext.EndpointBuilder);
    }

    /// <summary>
    /// Creates a <see cref="RequestDelegate{TRequestContext}"/> implementation for <paramref name="methodInfo"/>.
    /// </summary>
    /// <param name="methodInfo">A request handler with any number of custom parameters that often produces a response with its return value.</param>
    /// <param name="targetFactory">Creates the <see langword="this"/> for the non-static method.</param>
    /// <param name="options">The <see cref="RequestDelegateFactoryOptions{TRequestContext}"/> used to configure the behavior of the handler.</param>
    /// <returns>The <see cref="RequestDelegate{TRequestContext}"/>.</returns>
    public static RequestDelegateResult<TRequestContext> Create(MethodInfo methodInfo, Func<TRequestContext, object>? targetFactory, RequestDelegateFactoryOptions<TRequestContext>? options)
    {
        return Create(methodInfo, targetFactory, options, metadataResult: null);
    }

    /// <summary>
    /// Creates a <see cref="RequestDelegate{TRequestContext}"/> implementation for <paramref name="methodInfo"/>.
    /// </summary>
    /// <param name="methodInfo">A request handler with any number of custom parameters that often produces a response with its return value.</param>
    /// <param name="targetFactory">Creates the <see langword="this"/> for the non-static method.</param>
    /// <param name="options">The <see cref="RequestDelegateFactoryOptions{TRequestContext}"/> used to configure the behavior of the handler.</param>
    /// <param name="metadataResult">
    /// The result returned from <see cref="InferMetadata(MethodInfo, RequestDelegateFactoryOptions<TRequestContext>?)"/> if that was used to inferring metadata before creating the final RequestDelegate.
    /// If <see langword="null"/>, this call to <see cref="Create(Delegate, RequestDelegateFactoryOptions<TRequestContext>?, RequestDelegateMetadataResult?)"/> method will infer the metadata that
    /// <see cref="InferMetadata(MethodInfo, RequestDelegateFactoryOptions<TRequestContext>?)"/> would have inferred for the same <see cref="Delegate.Method"/> and populate <see cref="RequestDelegateFactoryOptions.EndpointBuilder{TRequestContext}"/>
    /// with that metadata. Otherwise, this metadata inference will be skipped as this step has already been done.
    /// </param>
    /// <returns>The <see cref="RequestDelegate{TRequestContext}"/>.</returns>
    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public static RequestDelegateResult<TRequestContext> Create(MethodInfo methodInfo, Func<TRequestContext, object>? targetFactory = null, RequestDelegateFactoryOptions<TRequestContext>? options = null, RequestDelegateMetadataResult? metadataResult = null)
    {
        ArgumentNullException.ThrowIfNull(methodInfo);

        if (methodInfo.DeclaringType is null)
        {
            throw new ArgumentException($"{nameof(methodInfo)} does not have a declaring type.");
        }

        var factoryContext = CreateFactoryContext(options, metadataResult);
        RequestDelegate<TRequestContext> finalRequestDelegate;

        if (methodInfo.IsStatic)
        {
            var untargetableRequestDelegate = CreateTargetableRequestDelegate(methodInfo, targetExpression: null, factoryContext);

            // CreateTargetableRequestDelegate can only return null given a RequestDelegate passed into the other RDF.Create() overload.
            Debug.Assert(untargetableRequestDelegate is not null);

            finalRequestDelegate = requestContext=> untargetableRequestDelegate(null, requestContext);
        }
        else
        {
            targetFactory ??= context => Activator.CreateInstance(methodInfo.DeclaringType)!;

            var targetExpression = Expression.Convert(TargetExpr, methodInfo.DeclaringType);
            var targetableRequestDelegate = CreateTargetableRequestDelegate(methodInfo, targetExpression, factoryContext, context => targetFactory(context));

            // CreateTargetableRequestDelegate can only return null given a RequestDelegate passed into the other RDF.Create() overload.
            Debug.Assert(targetableRequestDelegate is not null);

            finalRequestDelegate = requestContext=> targetableRequestDelegate(targetFactory(requestContext), requestContext);
        }

        return CreateRequestDelegateResult(finalRequestDelegate, factoryContext.EndpointBuilder);
    }

    private static RequestDelegateFactoryContext<TRequestContext> CreateFactoryContext(RequestDelegateFactoryOptions<TRequestContext>? options, RequestDelegateMetadataResult? metadataResult = null, Delegate? handler = null)
    {
        if (metadataResult?.CachedFactoryContext is RequestDelegateFactoryContext<TRequestContext> cachedFactoryContext)
        {
            cachedFactoryContext.MetadataAlreadyInferred = true;
            // The handler was not passed in to the InferMetadata call that originally created this context.
            cachedFactoryContext.Handler = handler;
            return cachedFactoryContext;
        }

        var serviceProvider = options?.ServiceProvider ?? options?.EndpointBuilder?.ApplicationServices ?? EmptyServiceProvider.Instance;
        var endpointBuilder = options?.EndpointBuilder ?? new RdfEndpointBuilder(serviceProvider);
;

        var factoryContext = new RequestDelegateFactoryContext<TRequestContext>
        {
            Handler = handler,
            ServiceProvider = serviceProvider,
            ServiceProviderIsService = serviceProvider.GetService<IServiceProviderIsService>(),
            RouteParameters = options?.RouteParameterNames,
            ThrowOnBadRequest = options?.ThrowOnBadRequest ?? false,
            EndpointBuilder = endpointBuilder,
            MetadataAlreadyInferred = metadataResult is not null        };

        return factoryContext;
    }

    private static RequestDelegateResult<TRequestContext> CreateRequestDelegateResult(RequestDelegate<TRequestContext> finalRequestDelegate, EndpointBuilder<TRequestContext> endpointBuilder)
    {
        endpointBuilder.RequestDelegate = finalRequestDelegate;
        return new RequestDelegateResult<TRequestContext>(finalRequestDelegate, AsReadOnlyList(endpointBuilder.Metadata));
    }

    private static IReadOnlyList<object> AsReadOnlyList(IList<object> metadata)
    {
        if (metadata is IReadOnlyList<object> readOnlyList)
        {
            return readOnlyList;
        }

        return new List<object>(metadata);
    }

    private static Func<object?, TRequestContext, Task>? CreateTargetableRequestDelegate(
        MethodInfo methodInfo,
        Expression? targetExpression,
        RequestDelegateFactoryContext<TRequestContext> factoryContext,
        Expression<Func<TRequestContext, object?>>? targetFactory = null)
    {
        // Non void return type

        // Task Invoke(HttpContext httpContext)
        // {
        //     // Action parameters are bound from the request, services, etc... based on attribute and type information.
        //     return ExecuteTask(handler(...), httpContext);
        // }

        // void return type

        // Task Invoke(HttpContext httpContext)
        // {
        //     handler(...);
        //     return default;
        // }

        // If ArgumentExpressions is not null here, it's guaranteed we have already inferred metadata and we can reuse a lot of work.
        // The converse is not true. Metadata may have already been inferred even if ArgumentExpressions is null, but metadata
        // inference is skipped internally if necessary.
        factoryContext.ArgumentExpressions ??= CreateArgumentsAndInferMetadata(methodInfo, factoryContext);

        factoryContext.MethodCall = CreateMethodCall(methodInfo, targetExpression, factoryContext.ArgumentExpressions);
        EndpointFilterDelegate<TRequestContext>? filterPipeline = null;
        var returnType = methodInfo.ReturnType;

        // If there are filters registered on the route handler, then we update the method call and
        // return type associated with the request to allow for the filter invocation pipeline.
        if (factoryContext.EndpointBuilder.FilterFactories.Count > 0)
        {
            filterPipeline = CreateFilterPipeline(methodInfo, targetExpression, factoryContext, targetFactory);

            if (filterPipeline is not null)
            {
                Expression<Func<EndpointFilterInvocationContext<TRequestContext>, ValueTask<object?>>> invokePipeline = (context) => filterPipeline(context);
                returnType = typeof(ValueTask<object?>);
                // var filterContext = new EndpointFilterInvocationContext<string, int>(httpContext, name_local, int_local);
                // invokePipeline.Invoke(filterContext);
                factoryContext.MethodCall = Expression.Block(
                    new[] { InvokedFilterContextExpr },
                    Expression.Assign(
                        InvokedFilterContextExpr,
                        CreateEndpointFilterInvocationContextBase(factoryContext, factoryContext.ArgumentExpressions)),
                        Expression.Invoke(invokePipeline, InvokedFilterContextExpr)
                    );
            }
        }

        // return null for plain RequestDelegates that have not been modified by filters so we can just pass back the original RequestDelegate.
        if (filterPipeline is null && factoryContext.Handler is RequestDelegate<TRequestContext>)
        {
            return null;
        }

        var responseWritingMethodCall = factoryContext.ParamCheckExpressions.Count > 0 ?
            CreateParamCheckingResponseWritingMethodCall(returnType, factoryContext) :
            AddResponseWritingToMethodCall(factoryContext.MethodCall, returnType, factoryContext);

        if (factoryContext.UsingTempSourceString)
        {
            responseWritingMethodCall = Expression.Block(new[] { TempSourceStringExpr }, responseWritingMethodCall);
        }

        return HandleRequestBodyAndCompileRequestDelegate(responseWritingMethodCall, factoryContext);
    }

    private static Expression[] CreateArgumentsAndInferMetadata(MethodInfo methodInfo, RequestDelegateFactoryContext<TRequestContext> factoryContext)
    {
        // Add any default accepts metadata. This does a lot of reflection and expression tree building, so the results are cached in RequestDelegateFactoryOptions.FactoryContext
        // For later reuse in Create().
        var args = CreateArguments(methodInfo.GetParameters(), factoryContext);

        if (!factoryContext.MetadataAlreadyInferred)
        {

            PopulateBuiltInResponseTypeMetadata(methodInfo.ReturnType, factoryContext.EndpointBuilder);

            // Add metadata provided by the delegate return type and parameter types next, this will be more specific than inferred metadata from above
            EndpointMetadataPopulator<TRequestContext>.PopulateMetadata(methodInfo, factoryContext.EndpointBuilder, factoryContext.Parameters);
        }

        return args;
    }

    private static EndpointFilterDelegate<TRequestContext>? CreateFilterPipeline(MethodInfo methodInfo, Expression? targetExpression, RequestDelegateFactoryContext<TRequestContext> factoryContext, Expression<Func<TRequestContext, object?>>? targetFactory)
    {
        Debug.Assert(factoryContext.EndpointBuilder.FilterFactories.Count > 0);
        // httpContext.Response.StatusCode >= 400
        // ? Task.CompletedTask
        // : {
        //   handlerInvocation
        // }
        // To generate the handler invocation, we first create the
        // target of the handler provided to the route.
        //      target = targetFactory(httpContext);
        // This target is then used to generate the handler invocation like so;
        //      ((Type)target).MethodName(parameters);
        //  When `handler` returns an object, we generate the following wrapper
        //  to convert it to `ValueTask<object?>` as expected in the filter
        //  pipeline.
        //      ValueTask<object?>.FromResult(handler(EndpointFilterInvocationContext.GetArgument<string>(0), EndpointFilterInvocationContext.GetArgument<int>(1)));
        //  When the `handler` is a generic Task or ValueTask we await the task and
        //  create a `ValueTask<object?> from the resulting value.
        //      new ValueTask<object?>(await handler(EndpointFilterInvocationContext.GetArgument<string>(0), EndpointFilterInvocationContext.GetArgument<int>(1)));
        //  When the `handler` returns a void or a void-returning Task, then we return an EmptyHttpResult
        //  to as a ValueTask<object?>
        // }

        var argTypes = factoryContext.ArgumentTypes;
        var contextArgAccess = new Expression[argTypes.Length];

        for (var i = 0; i < argTypes.Length; i++)
        {
            // MakeGenericMethod + value type requires IsDynamicCodeSupported to be true.
            if (RuntimeFeature.IsDynamicCodeSupported)
            {
                // Register expressions containing the boxed and unboxed variants
                // of the route handler's arguments for use in EndpointFilterInvocationContext
                // construction and route handler invocation.
                // context.GetArgument<string>(0)
                // (string, name_local), (int, int_local)
                contextArgAccess[i] = Expression.Call(FilterContextExpr, EndpointFilterInvocationContextGetArgument.MakeGenericMethod(argTypes[i]), Expression.Constant(i));
            }
            else
            {
                // We box if dynamic code isn't supported
                contextArgAccess[i] = Expression.Convert(
                    Expression.Property(FilterContextArgumentsExpr, ListIndexer, Expression.Constant(i)),
                argTypes[i]);
            }
        }

        var handlerReturnMapping = MapHandlerReturnTypeToValueTask(
                        targetExpression is null
                            ? Expression.Call(methodInfo, contextArgAccess)
                            : Expression.Call(targetExpression, methodInfo, contextArgAccess),
                        methodInfo.ReturnType);
        var handlerInvocation = Expression.Block(
                    new[] { TargetExpr },
                    targetFactory == null
                        ? Expression.Empty()
                        : Expression.Assign(TargetExpr, Expression.Invoke(targetFactory, FilterContextRequestContextExpr)),
                    handlerReturnMapping
                );
        var filteredInvocation = Expression.Lambda<EndpointFilterDelegate<TRequestContext>>(
            Expression.Condition(
                Expression.IsTrue(FilterContextRequestContextIsFailureExpr),
                EmptyHttpResultValueTaskExpr,
                handlerInvocation),
            FilterContextExpr).Compile();
        var routeHandlerContext = new EndpointFilterFactoryContext
        {
            MethodInfo = methodInfo,
            ApplicationServices = factoryContext.EndpointBuilder.ApplicationServices,
        };

        var initialFilteredInvocation = filteredInvocation;

        for (var i = factoryContext.EndpointBuilder.FilterFactories.Count - 1; i >= 0; i--)
        {
            var currentFilterFactory = factoryContext.EndpointBuilder.FilterFactories[i];
            filteredInvocation = currentFilterFactory(routeHandlerContext, filteredInvocation);
        }

        // The filter factories have run without modifying per-request behavior, we can skip running the pipeline.
        // If a plain old RequestDelegate was passed in (with no generic parameter), we can just return it back directly now.
        if (ReferenceEquals(initialFilteredInvocation, filteredInvocation))
        {
            return null;
        }

        return filteredInvocation;
    }

    private static Expression MapHandlerReturnTypeToValueTask(Expression methodCall, Type returnType)
    {
        if (returnType == typeof(void))
        {
            return Expression.Block(methodCall, EmptyHttpResultValueTaskExpr);
        }
        else if (returnType == typeof(Task))
        {
            return Expression.Call(ExecuteTaskWithEmptyResultMethod, methodCall);
        }
        else if (returnType == typeof(ValueTask))
        {
            return Expression.Call(ExecuteValueTaskWithEmptyResultMethod, methodCall);
        }
        else if (returnType == typeof(ValueTask<object?>))
        {
            return methodCall;
        }
        else if (returnType.IsGenericType &&
                     returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
        {
            var typeArg = returnType.GetGenericArguments()[0];
            return Expression.Call(ValueTaskOfTToValueTaskOfObjectMethod.MakeGenericMethod(typeArg), methodCall);
        }
        else if (returnType.IsGenericType &&
                    returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var typeArg = returnType.GetGenericArguments()[0];
            return Expression.Call(TaskOfTToValueTaskOfObjectMethod.MakeGenericMethod(typeArg), methodCall);
        }
        else
        {
            return Expression.Call(WrapObjectAsValueTaskMethod, methodCall);
        }
    }

    private static ValueTask<object?> ValueTaskOfTToValueTaskOfObject<T>(ValueTask<T> valueTask)
    {
        static async ValueTask<object?> ExecuteAwaited(ValueTask<T> valueTask)
        {
            return await valueTask;
        }

        if (valueTask.IsCompletedSuccessfully)
        {
            return new ValueTask<object?>(valueTask.Result);
        }

        return ExecuteAwaited(valueTask);
    }

    private static ValueTask<object?> TaskOfTToValueTaskOfObject<T>(Task<T> task)
    {
        static async ValueTask<object?> ExecuteAwaited(Task<T> task)
        {
            return await task;
        }

        if (task.IsCompletedSuccessfully)
        {
            return new ValueTask<object?>(task.Result);
        }

        return ExecuteAwaited(task);
    }

    private static Expression[] CreateArguments(ParameterInfo[]? parameters, RequestDelegateFactoryContext<TRequestContext> factoryContext)
    {
        if (parameters is null || parameters.Length == 0)
        {
            return Array.Empty<Expression>();
        }

        var args = new Expression[parameters.Length];

        factoryContext.ArgumentTypes = new Type[parameters.Length];
        factoryContext.BoxedArgs = new Expression[parameters.Length];
        factoryContext.Parameters = new List<ParameterInfo>(parameters);

        for (var i = 0; i < parameters.Length; i++)
        {
            args[i] = CreateArgument(parameters[i], factoryContext);

            factoryContext.ArgumentTypes[i] = parameters[i].ParameterType;
            factoryContext.BoxedArgs[i] = Expression.Convert(args[i], typeof(object));
        }
        if (factoryContext.HasMultipleBodyParameters)
        {
            var errorMessage = BuildErrorMessageForMultipleBodyParameters(factoryContext);
            throw new InvalidOperationException(errorMessage);
        }

        return args;
    }

    private static Expression CreateMethodCall(MethodInfo methodInfo, Expression? target, Expression[] arguments) =>
        target is null ?
            Expression.Call(methodInfo, arguments) :
            Expression.Call(target, methodInfo, arguments);

    private static ValueTask<object?> WrapObjectAsValueTask(object? obj)
    {
        return ValueTask.FromResult<object?>(obj);
    }

    // If we're calling TryParse or validating parameter optionality and
    // wasParamCheckFailure indicates it failed, set a 400 StatusCode instead of calling the method.
    private static Expression CreateParamCheckingResponseWritingMethodCall(Type returnType, RequestDelegateFactoryContext<TRequestContext> factoryContext)
    {
        // {
        //     string tempSourceString;
        //     bool wasParamCheckFailure = false;
        //
        //     // Assume "int param1" is the first parameter, "[FromRoute] int? param2 = 42" is the second parameter ...
        //     int param1_local;
        //     int? param2_local;
        //     // ...
        //
        //     tempSourceString = httpContext.RouteValue["param1"] ?? httpContext.Query["param1"];
        //
        //     if (tempSourceString != null)
        //     {
        //         if (!int.TryParse(tempSourceString, out param1_local))
        //         {
        //             wasParamCheckFailure = true;
        //             Log.ParameterBindingFailed(httpContext, "Int32", "id", tempSourceString)
        //         }
        //     }
        //
        //     tempSourceString = httpContext.RouteValue["param2"];
        //     // ...
        //
        //     return wasParamCheckFailure ?
        //         {
        //              httpContext.Response.StatusCode = 400;
        //              return Task.CompletedTask;
        //         } :
        //         {
        //             // Logic generated by AddResponseWritingToMethodCall() that calls handler(param1_local, param2_local, ...)
        //         };
        // }

        var localVariables = new ParameterExpression[factoryContext.ExtraLocals.Count + 1];
        var checkParamAndCallMethod = new Expression[factoryContext.ParamCheckExpressions.Count + 1];

        for (var i = 0; i < factoryContext.ExtraLocals.Count; i++)
        {
            localVariables[i] = factoryContext.ExtraLocals[i];
        }

        for (var i = 0; i < factoryContext.ParamCheckExpressions.Count; i++)
        {
            checkParamAndCallMethod[i] = factoryContext.ParamCheckExpressions[i];
        }

        localVariables[factoryContext.ExtraLocals.Count] = WasParamCheckFailureExpr;

        // If filters have been registered, we set the `wasParamCheckFailure` property
        // but do not return from the invocation to allow the filters to run.
        if (factoryContext.EndpointBuilder.FilterFactories.Count > 0)
        {
            // if (wasParamCheckFailure)
            // {
            //   httpContext.Response.StatusCode = 400;
            // }
            // return RequestDelegateFactory.ExecuteObjectReturn(invocationPipeline.Invoke(context) as object);
            var checkWasParamCheckFailureWithFilters = Expression.Block(
                Expression.IfThen(
                    WasParamCheckFailureExpr,
                    Expression.Assign(IsFailureExpr, Expression.Constant(true))),
                AddResponseWritingToMethodCall(factoryContext.MethodCall!, returnType, factoryContext)
            );

            checkParamAndCallMethod[factoryContext.ParamCheckExpressions.Count] = checkWasParamCheckFailureWithFilters;
        }
        else
        {
            // wasParamCheckFailure ? {
            //  httpContext.Response.StatusCode = 400;
            //  return Task.CompletedTask;
            // } : {
            //  return RequestDelegateFactory.ExecuteObjectReturn(invocationPipeline.Invoke(context) as object);
            // }
            var checkWasParamCheckFailure = Expression.Condition(
                WasParamCheckFailureExpr,
                Expression.Block(
                    Expression.Assign(IsFailureExpr, Expression.Constant(true)),
                    CompletedTaskExpr),
                AddResponseWritingToMethodCall(factoryContext.MethodCall!, returnType, factoryContext));
            checkParamAndCallMethod[factoryContext.ParamCheckExpressions.Count] = checkWasParamCheckFailure;
        }

        return Expression.Block(localVariables, checkParamAndCallMethod);
    }

    private static Expression AddResponseWritingToMethodCall(Expression methodCall, Type returnType, RequestDelegateFactoryContext<TRequestContext> factoryContext)
    {
        // Exact request delegate match
        if (returnType == typeof(void))
        {
            return Expression.Block(methodCall, CompletedTaskExpr);
        }
        else if (returnType == typeof(object))
        {
            return Expression.Call(
                ExecuteAwaitedReturnMethod,
                methodCall,
                HttpContextExpr);
        }
        else if (returnType == typeof(ValueTask<object>))
        {
            return Expression.Call(ExecuteValueTaskOfObjectMethod,
                methodCall,
                HttpContextExpr);
        }
        else if (returnType == typeof(Task<object>))
        {
            return Expression.Call(ExecuteTaskOfObjectMethod,
                methodCall,
                HttpContextExpr);
        }
        else if (AwaitableInfo.IsTypeAwaitable(returnType, out _))
        {
            if (returnType == typeof(Task))
            {
                return methodCall;
            }
            else if (returnType == typeof(ValueTask))
            {
                return Expression.Call(
                    ExecuteValueTaskMethod,
                    methodCall);
            }
            else if (returnType.IsGenericType &&
                     returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var typeArg = returnType.GetGenericArguments()[0];

                if (typeof(IResult<TRequestContext>).IsAssignableFrom(typeArg))
                {
                    return Expression.Call(
                        ExecuteTaskResultOfTMethod.MakeGenericMethod(typeArg),
                        methodCall,
                        HttpContextExpr);
                }
                // ExecuteTask<T>(handler(..), httpContext);
                else                 {

                    return Expression.Call(
                        ExecuteTaskOfTMethod.MakeGenericMethod(typeArg),
                        methodCall,
                        HttpContextExpr);
                }
            }
            else if (returnType.IsGenericType &&
                     returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                var typeArg = returnType.GetGenericArguments()[0];

                if (typeof(IResult<TRequestContext>).IsAssignableFrom(typeArg))
                {
                    return Expression.Call(
                        ExecuteValueResultTaskOfTMethod.MakeGenericMethod(typeArg),
                        methodCall,
                        HttpContextExpr);
                }
                // ExecuteTask<T>(handler(..), httpContext);
                else                 {

                    return Expression.Call(
                        ExecuteValueTaskOfTMethod.MakeGenericMethod(typeArg),
                        methodCall,
                        HttpContextExpr);
                }
            }
            else
            {
                // TODO: Handle custom awaitables
                throw GetUnsupportedReturnTypeException(returnType);
            }
        }
        else if (typeof(IResult<TRequestContext>).IsAssignableFrom(returnType))
        {
            if (returnType.IsValueType)
            {
                var box = Expression.TypeAs(methodCall, typeof(IResult<TRequestContext>));
                return Expression.Call(ResultWriteResponseAsyncMethod, box, HttpContextExpr);
            }
            return Expression.Call(ResultWriteResponseAsyncMethod, methodCall, HttpContextExpr);
        }
        else if (returnType.IsByRefLike)
        {
            throw GetUnsupportedReturnTypeException(returnType);
        }
        else
        {

            return Expression.Call(
                ObjectResultWriteResponseOfTAsyncMethod.MakeGenericMethod(returnType),
                HttpContextExpr,
                methodCall);
        }
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern",
        Justification = "CreateValueType is only called on a ValueType. You can always create an instance of a ValueType.")]
    private static object? CreateValueType(Type t) => RuntimeHelpers.GetUninitializedObject(t);

    private static Expression GetValueFromProperty(MemberExpression sourceExpression, PropertyInfo itemProperty, string key, Type? returnType = null)
    {
        var indexArguments = new[] { Expression.Constant(key) };
        var indexExpression = Expression.MakeIndex(sourceExpression, itemProperty, indexArguments);
        return Expression.Convert(indexExpression, returnType ?? typeof(string));
    }

    private static Expression BindParameterFromProperties(ParameterInfo parameter, RequestDelegateFactoryContext<TRequestContext> factoryContext)
    {
        var parameterType = parameter.ParameterType;
        var isNullable = Nullable.GetUnderlyingType(parameterType) != null ||
            factoryContext.NullabilityContext.Create(parameter)?.ReadState == NullabilityState.Nullable;

        if (isNullable)
        {
            throw new InvalidOperationException($"The nullable type '{TypeNameHelper.GetTypeDisplayName(parameter.ParameterType, fullName: false)}' is not supported, mark the parameter as non-nullable.");
        }

        var argumentExpression = Expression.Variable(parameter.ParameterType, $"{parameter.Name}_local");
        var (constructor, parameters) = ParameterBindingMethodCache.FindConstructor(parameterType);

        Expression initExpression;

        if (constructor is not null && parameters is { Length: > 0 })
        {
            //  arg_local = new T(....)

            var constructorArguments = new Expression[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameterInfo =
                    new PropertyAsParameterInfo<TRequestContext>(parameters[i].PropertyInfo, parameters[i].ParameterInfo, factoryContext.NullabilityContext);
                constructorArguments[i] = CreateArgument(parameterInfo, factoryContext);
                factoryContext.Parameters.Add(parameterInfo);
            }

            initExpression = Expression.New(constructor, constructorArguments);
        }
        else
        {
            //  arg_local = new T()
            //  {
            //      arg_local.Property[0] = expression[0],
            //      arg_local.Property[n] = expression[n],
            //  }

            var properties = parameterType.GetProperties();
            var bindings = new List<MemberBinding>(properties.Length);

            for (var i = 0; i < properties.Length; i++)
            {
                // For parameterless ctor we will init only writable properties.
                if (properties[i].CanWrite && properties[i].GetSetMethod(nonPublic: false) != null)
                {
                    var parameterInfo = new PropertyAsParameterInfo<TRequestContext>(properties[i], factoryContext.NullabilityContext);
                    bindings.Add(Expression.Bind(properties[i], CreateArgument(parameterInfo, factoryContext)));
                    factoryContext.Parameters.Add(parameterInfo);
                }
            }

            var newExpression = constructor is null ?
                Expression.New(parameterType) :
                Expression.New(constructor);

            initExpression = Expression.MemberInit(newExpression, bindings);
        }

        factoryContext.ParamCheckExpressions.Add(
            Expression.Assign(argumentExpression, initExpression));

        factoryContext.TrackedParameters.Add(parameter.Name!, RequestDelegateFactoryConstants.PropertyAsParameter);
        factoryContext.ExtraLocals.Add(argumentExpression);

        return argumentExpression;
    }

    private static Expression BindParameterFromService(ParameterInfo parameter, RequestDelegateFactoryContext<TRequestContext> factoryContext)
    {
        var isOptional = IsOptionalParameter(parameter, factoryContext);

        if (isOptional)
        {
            return Expression.Call(GetServiceMethod.MakeGenericMethod(parameter.ParameterType), RequestServicesExpr);
        }
        return Expression.Call(GetRequiredServiceMethod.MakeGenericMethod(parameter.ParameterType), RequestServicesExpr);
    }

    private static Expression BindParameterFromKeyedService(ParameterInfo parameter, object key, RequestDelegateFactoryContext<TRequestContext> factoryContext)
    {
        var isOptional = IsOptionalParameter(parameter, factoryContext);

        if (isOptional)
        {
            return Expression.Call(GetKeyedServiceMethod.MakeGenericMethod(parameter.ParameterType), RequestServicesExpr, Expression.Convert(
                Expression.Constant(key),
                typeof(object)));
        }
        return Expression.Call(GetRequiredKeyedServiceMethod.MakeGenericMethod(parameter.ParameterType), RequestServicesExpr, Expression.Convert(
            Expression.Constant(key),
            typeof(object)));
    }

    private static Expression BindParameterFromValue(ParameterInfo parameter, Expression valueExpression, RequestDelegateFactoryContext<TRequestContext> factoryContext, string source)
    {
        if (parameter.ParameterType == typeof(string) || parameter.ParameterType == typeof(string[])
            || parameter.ParameterType == typeof(StringValues) || parameter.ParameterType == typeof(StringValues?))
        {
            return BindParameterFromExpression(parameter, valueExpression, factoryContext, source);
        }

        var isOptional = IsOptionalParameter(parameter, factoryContext);
        var argument = Expression.Variable(parameter.ParameterType, $"{parameter.Name}_local");

        var parameterTypeNameConstant = Expression.Constant(TypeNameHelper.GetTypeDisplayName(parameter.ParameterType, fullName: false));
        var parameterNameConstant = Expression.Constant(parameter.Name);
        var sourceConstant = Expression.Constant(source);

        factoryContext.UsingTempSourceString = true;

        var targetParseType = parameter.ParameterType.IsArray ? parameter.ParameterType.GetElementType()! : parameter.ParameterType;

        var underlyingNullableType = Nullable.GetUnderlyingType(targetParseType);
        var isNotNullable = underlyingNullableType is null;

        var nonNullableParameterType = underlyingNullableType ?? targetParseType;
        var tryParseMethodCall = ParameterBindingMethodCache.FindTryParseMethod(nonNullableParameterType);

        if (tryParseMethodCall is null)
        {
            var typeName = TypeNameHelper.GetTypeDisplayName(targetParseType, fullName: false);
            throw new InvalidOperationException($"{parameter.Name} must have a valid TryParse method to support converting from a string. No public static bool {typeName}.TryParse(string, out {typeName}) method found for {parameter.Name}.");
        }

        // string tempSourceString;
        // bool wasParamCheckFailure = false;
        //
        // // Assume "int param1" is the first parameter and "[FromRoute] int? param2 = 42" is the second parameter.
        // int param1_local;
        // int? param2_local;
        //
        // tempSourceString = httpContext.RouteValue["param1"] ?? httpContext.Query["param1"];
        //
        // if (tempSourceString != null)
        // {
        //     if (!int.TryParse(tempSourceString, out param1_local))
        //     {
        //         wasParamCheckFailure = true;
        //         Log.ParameterBindingFailed(httpContext, "Int32", "id", tempSourceString)
        //     }
        // }
        //
        // tempSourceString = httpContext.RouteValue["param2"];
        //
        // if (tempSourceString != null)
        // {
        //     if (int.TryParse(tempSourceString, out int parsedValue))
        //     {
        //         param2_local = parsedValue;
        //     }
        //     else
        //     {
        //         wasParamCheckFailure = true;
        //         Log.ParameterBindingFailed(httpContext, "Int32", "id", tempSourceString)
        //     }
        // }
        // else
        // {
        //     param2_local = 42;
        // }

        // string[]? values = httpContext.Request.Query["param1"].ToArray();
        // int[] param_local = values.Length > 0 ? new int[values.Length] : Array.Empty<int>();

        // if (values != null)
        // {
        //     int index = 0;
        //     while (index < values.Length)
        //     {
        //         tempSourceString = values[i];
        //         if (int.TryParse(tempSourceString, out var parsedValue))
        //         {
        //             param_local[i] = parsedValue;
        //         }
        //         else
        //         {
        //             wasParamCheckFailure = true;
        //             Log.ParameterBindingFailed(httpContext, "Int32[]", "param1", tempSourceString);
        //             break;
        //         }
        //
        //         index++
        //     }
        // }

        // If the parameter is nullable, create a "parsedValue" local to TryParse into since we cannot use the parameter directly.
        var parsedValue = Expression.Variable(nonNullableParameterType, "parsedValue");

        var failBlock = Expression.Block(
            Expression.Assign(WasParamCheckFailureExpr, Expression.Constant(true)),
            Expression.Call(LogParameterBindingFailedMethod,
                HttpContextExpr, parameterTypeNameConstant, parameterNameConstant,
                TempSourceStringExpr, Expression.Constant(factoryContext.ThrowOnBadRequest)));

        var tryParseCall = tryParseMethodCall(parsedValue, Expression.Constant(CultureInfo.InvariantCulture));

        // The following code is generated if the parameter is required and
        // the method should not be matched.
        //
        // if (tempSourceString == null)
        // {
        //      wasParamCheckFailure = true;
        //      Log.RequiredParameterNotProvided(httpContext, "Int32", "param1");
        // }
        var checkRequiredParaseableParameterBlock = Expression.Block(
            Expression.IfThen(TempSourceStringNullExpr,
                Expression.Block(
                    Expression.Assign(WasParamCheckFailureExpr, Expression.Constant(true)),
                    Expression.Call(LogRequiredParameterNotProvidedMethod,
                        HttpContextExpr, parameterTypeNameConstant, parameterNameConstant, sourceConstant,
                        Expression.Constant(factoryContext.ThrowOnBadRequest))
                )
            )
        );

        var index = Expression.Variable(typeof(int), "index");

        // If the parameter is nullable, we need to assign the "parsedValue" local to the nullable parameter on success.
        var tryParseExpression = Expression.Block(new[] { parsedValue },
                Expression.IfThenElse(tryParseCall,
                    Expression.Assign(parameter.ParameterType.IsArray ? Expression.ArrayAccess(argument, index) : argument, Expression.Convert(parsedValue, targetParseType)),
                    failBlock));

        var ifNotNullTryParse = !parameter.HasDefaultValue
            ? Expression.IfThen(TempSourceStringNotNullExpr, tryParseExpression)
            : Expression.IfThenElse(TempSourceStringNotNullExpr, tryParseExpression,
                Expression.Assign(argument,
                Expression.Constant(parameter.DefaultValue, parameter.ParameterType)));

        var loopExit = Expression.Label();

        // REVIEW: We can reuse this like we reuse temp source string
        var stringArrayExpr = parameter.ParameterType.IsArray ? Expression.Variable(typeof(string[]), "tempStringArray") : null;
        var elementTypeNullabilityInfo = parameter.ParameterType.IsArray ? factoryContext.NullabilityContext.Create(parameter)?.ElementType : null;

        // Determine optionality of the element type of the array
        var elementTypeOptional = !isNotNullable || (elementTypeNullabilityInfo?.ReadState != NullabilityState.NotNull);

        // The loop that populates the resulting array values
        var arrayLoop = parameter.ParameterType.IsArray ? Expression.Block(
                        // param_local = new int[values.Length];
                        Expression.Assign(argument, Expression.NewArrayBounds(parameter.ParameterType.GetElementType()!, Expression.ArrayLength(stringArrayExpr!))),
                        // index = 0
                        Expression.Assign(index, Expression.Constant(0)),
                        // while (index < values.Length)
                        Expression.Loop(
                            Expression.Block(
                                Expression.IfThenElse(
                                    Expression.LessThan(index, Expression.ArrayLength(stringArrayExpr!)),
                                        // tempSourceString = values[index];
                                        Expression.Block(
                                            Expression.Assign(TempSourceStringExpr, Expression.ArrayIndex(stringArrayExpr!, index)),
                                            elementTypeOptional ? Expression.IfThen(TempSourceStringIsNotNullOrEmptyExpr, tryParseExpression)
                                                                : tryParseExpression
                                        ),
                                       // else break
                                       Expression.Break(loopExit)
                                 ),
                                 // index++
                                 Expression.PostIncrementAssign(index)
                            )
                        , loopExit)
                    ) : null;

        var fullParamCheckBlock = (parameter.ParameterType.IsArray, isOptional) switch
        {
            // (isArray: true, optional: true)
            (true, true) =>

            Expression.Block(
                new[] { index, stringArrayExpr! },
                // values = httpContext.Request.Query["id"];
                Expression.Assign(stringArrayExpr!, valueExpression),
                Expression.IfThen(
                    Expression.NotEqual(stringArrayExpr!, Expression.Constant(null)),
                    arrayLoop!
                )
            ),

            // (isArray: true, optional: false)
            (true, false) =>

            Expression.Block(
                new[] { index, stringArrayExpr! },
                // values = httpContext.Request.Query["id"];
                Expression.Assign(stringArrayExpr!, valueExpression),
                Expression.IfThenElse(
                    Expression.NotEqual(stringArrayExpr!, Expression.Constant(null)),
                    arrayLoop!,
                    failBlock
                )
            ),

            // (isArray: false, optional: false)
            (false, false) =>

            Expression.Block(
                // tempSourceString = httpContext.RequestValue["id"];
                Expression.Assign(TempSourceStringExpr, valueExpression),
                // if (tempSourceString == null) { ... } only produced when parameter is required
                checkRequiredParaseableParameterBlock,
                // if (tempSourceString != null) { ... }
                ifNotNullTryParse),

            // (isArray: false, optional: true)
            (false, true) =>

            Expression.Block(
                // tempSourceString = httpContext.RequestValue["id"];
                Expression.Assign(TempSourceStringExpr, valueExpression),
                // if (tempSourceString != null) { ... }
                ifNotNullTryParse)
        };

        factoryContext.ExtraLocals.Add(argument);
        factoryContext.ParamCheckExpressions.Add(fullParamCheckBlock);

        return argument;
    }

    private static Expression BindParameterFromExpression(
        ParameterInfo parameter,
        Expression valueExpression,
        RequestDelegateFactoryContext<TRequestContext> factoryContext,
        string source)
    {
        var nullability = factoryContext.NullabilityContext.Create(parameter);
        var isOptional = IsOptionalParameter(parameter, factoryContext);

        var argument = Expression.Variable(parameter.ParameterType, $"{parameter.Name}_local");

        var parameterTypeNameConstant = Expression.Constant(TypeNameHelper.GetTypeDisplayName(parameter.ParameterType, fullName: false));
        var parameterNameConstant = Expression.Constant(parameter.Name);
        var sourceConstant = Expression.Constant(source);

        if (!isOptional)
        {
            // The following is produced if the parameter is required:
            //
            // argument = value["param1"];
            // if (argument == null)
            // {
            //      wasParamCheckFailure = true;
            //      Log.RequiredParameterNotProvided(httpContext, "TypeOfValue", "param1");
            // }
            var checkRequiredStringParameterBlock = Expression.Block(
                Expression.Assign(argument, valueExpression),
                Expression.IfThen(Expression.Equal(argument, Expression.Constant(null)),
                    Expression.Block(
                        Expression.Assign(WasParamCheckFailureExpr, Expression.Constant(true)),
                        Expression.Call(LogRequiredParameterNotProvidedMethod,
                            HttpContextExpr, parameterTypeNameConstant, parameterNameConstant, sourceConstant,
                            Expression.Constant(factoryContext.ThrowOnBadRequest))
                    )
                )
            );

            // NOTE: when StringValues is used as a parameter, value["some_unpresent_parameter"] returns StringValue.Empty, and it's equivalent to (string?)null

            factoryContext.ExtraLocals.Add(argument);
            factoryContext.ParamCheckExpressions.Add(checkRequiredStringParameterBlock);
            return argument;
        }

        // Allow nullable parameters that don't have a default value
        if (nullability.ReadState != NullabilityState.NotNull && !parameter.HasDefaultValue)
        {
            if (parameter.ParameterType == typeof(StringValues?))
            {
                // when Nullable<StringValues> is used and the actual value is StringValues.Empty, we should pass in a Nullable<StringValues>
                return Expression.Block(
                    Expression.Condition(Expression.Equal(valueExpression, Expression.Convert(Expression.Constant(StringValues.Empty), parameter.ParameterType)),
                            Expression.Convert(Expression.Constant(null), parameter.ParameterType),
                            valueExpression
                        )
                    );
            }
            return valueExpression;
        }

        // The following is produced if the parameter is optional. Note that we convert the
        // default value to the target ParameterType to address scenarios where the user is
        // is setting null as the default value in a context where nullability is disabled.
        //
        // param1_local = httpContext.RouteValue["param1"] ?? httpContext.Query["param1"];
        // param1_local != null ? param1_local : Convert(null, Int32)
        return Expression.Block(
            Expression.Condition(Expression.NotEqual(valueExpression, Expression.Constant(null)),
                valueExpression,
                Expression.Convert(Expression.Constant(parameter.DefaultValue), parameter.ParameterType)));
    }

    private static Expression BindParameterFromProperty(ParameterInfo parameter, MemberExpression property, PropertyInfo itemProperty, string key, RequestDelegateFactoryContext<TRequestContext> factoryContext, string source) =>
        BindParameterFromValue(parameter, GetValueFromProperty(property, itemProperty, key, GetExpressionType(parameter.ParameterType)), factoryContext, source);

    private static Type? GetExpressionType(Type type) =>
        type.IsArray ? typeof(string[]) :
        type == typeof(StringValues) ? typeof(StringValues) :
        type == typeof(StringValues?) ? typeof(StringValues?) :
        null;

    private static Expression BindParameterFromRouteValueOrQueryString(ParameterInfo parameter, string key, RequestDelegateFactoryContext<TRequestContext> factoryContext)
    {
        var routeValue = GetValueFromProperty(RouteValuesExpr, RouteValuesIndexerProperty, key);
        var queryValue = GetValueFromProperty(QueryExpr, QueryIndexerProperty, key);
        return BindParameterFromValue(parameter, Expression.Coalesce(routeValue, queryValue), factoryContext, "route or query string");
    }

    private static Expression BindParameterFromBindAsync(ParameterInfo parameter, RequestDelegateFactoryContext<TRequestContext> factoryContext)
    {
        // We reference the boundValues array by parameter index here
        var isOptional = IsOptionalParameter(parameter, factoryContext);

        // Get the BindAsync method for the type.
        var bindAsyncMethod = ParameterBindingMethodCache.FindBindAsyncMethod(parameter);
        // We know BindAsync exists because there's no way to opt-in without defining the method on the type.
        Debug.Assert(bindAsyncMethod.Expression is not null);

        // Compile the delegate to the BindAsync method for this parameter index
        var bindAsyncDelegate = Expression.Lambda<Func<TRequestContext, ValueTask<object?>>>(bindAsyncMethod.Expression, HttpContextExpr).Compile();
        factoryContext.ParameterBinders.Add(bindAsyncDelegate);

        // boundValues[index]
        var boundValueExpr = Expression.ArrayIndex(BoundValuesArrayExpr, Expression.Constant(factoryContext.ParameterBinders.Count - 1));

        if (!isOptional)
        {
            var typeName = TypeNameHelper.GetTypeDisplayName(parameter.ParameterType, fullName: false);
            var message = bindAsyncMethod.ParamCount == 2 ? $"{typeName}.BindAsync(HttpContext, ParameterInfo)" : $"{typeName}.BindAsync(HttpContext)";
            var checkRequiredBodyBlock = Expression.Block(
                    Expression.IfThen(
                    Expression.Equal(boundValueExpr, Expression.Constant(null)),
                        Expression.Block(
                            Expression.Assign(WasParamCheckFailureExpr, Expression.Constant(true)),
                            Expression.Call(LogRequiredParameterNotProvidedMethod,
                                    HttpContextExpr,
                                    Expression.Constant(typeName),
                                    Expression.Constant(parameter.Name),
                                    Expression.Constant(message),
                                    Expression.Constant(factoryContext.ThrowOnBadRequest))
                        )
                    )
                );

            factoryContext.ParamCheckExpressions.Add(checkRequiredBodyBlock);
        }

        // (ParameterType)boundValues[i]
        return Expression.Convert(boundValueExpr, parameter.ParameterType);
    }

    private static bool IsOptionalParameter(ParameterInfo parameter, RequestDelegateFactoryContext<TRequestContext> factoryContext)
    {
        if (parameter is PropertyAsParameterInfo<TRequestContext> argument)
        {
            return argument.IsOptional;
        }

        // - Parameters representing value or reference types with a default value
        // under any nullability context are treated as optional.
        // - Value type parameters without a default value in an oblivious
        // nullability context are required.
        // - Reference type parameters without a default value in an oblivious
        // nullability context are optional.
        var nullabilityInfo = factoryContext.NullabilityContext.Create(parameter);
        return parameter.HasDefaultValue
            || nullabilityInfo.ReadState != NullabilityState.NotNull;
    }

    private static MethodInfo GetMethodInfo<T>(Expression<T> expr)
    {
        var mc = (MethodCallExpression)expr.Body;
        return mc.Method;
    }

    private static MemberInfo GetMemberInfo<T>(Expression<T> expr)
    {
        var mc = (MemberExpression)expr.Body;
        return mc.Member;
    }

    // The result of the method is null so we fallback to some runtime logic.
    // First we check if the result is IResult, Task<IResult> or ValueTask<IResult>. If
    // it is, we await if necessary then execute the result.
    // Then we check to see if it's Task<object> or ValueTask<object>. If it is, we await
    // if necessary and restart the cycle until we've reached a terminal state (unknown type).
    // We currently don't handle Task<unknown> or ValueTask<unknown>. We can support this later if this
    // ends up being a common scenario.
    private static Task ExecuteValueTaskOfObject(ValueTask<object> valueTask, TRequestContext requestContext)
    {
        static async Task ExecuteAwaited(ValueTask<object> valueTask, TRequestContext requestContext)
        {
            await ExecuteAwaitedReturn(await valueTask, requestContext);
        }

        if (valueTask.IsCompletedSuccessfully)
        {
            return ExecuteAwaitedReturn(valueTask.GetAwaiter().GetResult(), requestContext);
        }

        return ExecuteAwaited(valueTask, requestContext);
    }

    private static Task ExecuteTaskOfObject(Task<object> task, TRequestContext requestContext)
    {
        static async Task ExecuteAwaited(Task<object> task, TRequestContext requestContext)
        {
            await ExecuteAwaitedReturn(await task, requestContext);
        }

        if (task.IsCompletedSuccessfully)
        {
            return ExecuteAwaitedReturn(task.GetAwaiter().GetResult(), requestContext);
        }

        return ExecuteAwaited(task, requestContext);
    }

    private static Task ExecuteValueTask(ValueTask task)
    {
        static async Task ExecuteAwaited(ValueTask task)
        {
            await task;
        }

        if (task.IsCompletedSuccessfully)
        {
            task.GetAwaiter().GetResult();
            return Task.CompletedTask;
        }

        return ExecuteAwaited(task);
    }

    private static ValueTask<object?> ExecuteTaskWithEmptyResult(Task task)
    {
        static async ValueTask<object?> ExecuteAwaited(Task task)
        {
            await task;
            return EmptyResult<TRequestContext>.Instance;
        }

        if (task.IsCompletedSuccessfully)
        {
            return new ValueTask<object?>(EmptyResult<TRequestContext>.Instance);
        }

        return ExecuteAwaited(task);
    }

    private static ValueTask<object?> ExecuteValueTaskWithEmptyResult(ValueTask valueTask)
    {
        static async ValueTask<object?> ExecuteAwaited(ValueTask task)
        {
            await task;
            return EmptyResult<TRequestContext>.Instance;
        }

        if (valueTask.IsCompletedSuccessfully)
        {
            valueTask.GetAwaiter().GetResult();
            return new ValueTask<object?>(EmptyResult<TRequestContext>.Instance);
        }

        return ExecuteAwaited(valueTask);
    }

    private static Task ExecuteValueTaskResult<T>(ValueTask<T?> task, TRequestContext requestContext) where T : IResult<TRequestContext>
    {
        static async Task ExecuteAwaited(ValueTask<T> task, TRequestContext requestContext)
        {
            await EnsureRequestResultNotNull(await task).ExecuteAsync(requestContext);
        }

        if (task.IsCompletedSuccessfully)
        {
            return EnsureRequestResultNotNull(task.GetAwaiter().GetResult()).ExecuteAsync(requestContext);
        }

        return ExecuteAwaited(task!, requestContext);
    }

    private static async Task ExecuteTaskResult<T>(Task<T?> task, TRequestContext requestContext) where T : IResult<TRequestContext>
    {
        EnsureRequestTaskOfNotNull(task);

        await EnsureRequestResultNotNull(await task).ExecuteAsync(requestContext);
    }

    private static async Task ExecuteResultWriteResponse(IResult<TRequestContext>? result, TRequestContext requestContext)
    {
        await EnsureRequestResultNotNull(result).ExecuteAsync(requestContext);
    }

    private static NotSupportedException GetUnsupportedReturnTypeException(Type returnType)
    {
        return new NotSupportedException($"Unsupported return type: {TypeNameHelper.GetTypeDisplayName(returnType)}");
    }

    private static class RequestDelegateFactoryConstants
    {
        public const string RouteAttribute = "Route (Attribute)";
        public const string QueryAttribute = "Query (Attribute)";
        public const string HeaderAttribute = "Header (Attribute)";
        public const string BodyAttribute = "Body (Attribute)";
        public const string ServiceAttribute = "Service (Attribute)";
        public const string FormFileAttribute = "Form File (Attribute)";
        public const string FormAttribute = "Form (Attribute)";
        public const string FormBindingAttribute = "Form Binding (Attribute)";
        public const string RouteParameter = "Route (Inferred)";
        public const string QueryStringParameter = "Query String (Inferred)";
        public const string ServiceParameter = "Services (Inferred)";
        public const string BodyParameter = "Body (Inferred)";
        public const string RouteOrQueryStringParameter = "Route or Query String (Inferred)";
        public const string FormFileParameter = "Form File (Inferred)";
        public const string FormCollectionParameter = "Form Collection (Inferred)";
        public const string PropertyAsParameter = "As Parameter (Attribute)";
    }

    private static partial class Log
    {

        public static void ParameterBindingFailed(TRequestContext requestContext, string parameterTypeName, string parameterName, string sourceValue, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, RequestDelegateCreationLogging.ParameterBindingFailedExceptionMessage, parameterTypeName, parameterName, sourceValue);
                throw new BadRequestException(message);
            }

            ParameterBindingFailed(GetLogger(requestContext), parameterTypeName, parameterName, sourceValue);
        }

        [LoggerMessage(RequestDelegateCreationLogging.ParameterBindingFailedEventId, LogLevel.Debug, RequestDelegateCreationLogging.ParameterBindingFailedLogMessage, EventName = RequestDelegateCreationLogging.ParameterBindingFailedEventName)]
        private static partial void ParameterBindingFailed(ILogger logger, string parameterType, string parameterName, string sourceValue);

        public static void RequiredParameterNotProvided(TRequestContext requestContext, string parameterTypeName, string parameterName, string source, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, RequestDelegateCreationLogging.RequiredParameterNotProvidedExceptionMessage, parameterTypeName, parameterName, source);
                throw new BadRequestException(message);
            }

            RequiredParameterNotProvided(GetLogger(requestContext), parameterTypeName, parameterName, source);
        }

        [LoggerMessage(RequestDelegateCreationLogging.RequiredParameterNotProvidedEventId, LogLevel.Debug, RequestDelegateCreationLogging.RequiredParameterNotProvidedLogMessage, EventName = RequestDelegateCreationLogging.RequiredParameterNotProvidedEventName)]
        private static partial void RequiredParameterNotProvided(ILogger logger, string parameterType, string parameterName, string source);

        public static void ImplicitBodyNotProvided(TRequestContext requestContext, string parameterName, bool shouldThrow)
        {
            if (shouldThrow)
            {
                var message = string.Format(CultureInfo.InvariantCulture, RequestDelegateCreationLogging.ImplicitBodyNotProvidedExceptionMessage, parameterName);
                throw new BadRequestException(message);
            }

            ImplicitBodyNotProvided(GetLogger(requestContext), parameterName);
        }

        [LoggerMessage(RequestDelegateCreationLogging.ImplicitBodyNotProvidedEventId, LogLevel.Debug, RequestDelegateCreationLogging.ImplicitBodyNotProvidedLogMessage, EventName = RequestDelegateCreationLogging.ImplicitBodyNotProvidedEventName)]
        private static partial void ImplicitBodyNotProvided(ILogger logger, string parameterName);

        private static ILogger GetLogger(TRequestContext requestContext)
        {
            var loggerFactory = requestContext.RequestServices.GetRequiredService<ILoggerFactory>();
            return loggerFactory.CreateLogger(typeof(RequestDelegateFactory<TRequestContext>));
        }
    }

    private static void EnsureRequestTaskOfNotNull<T>(Task<T?> task) where T : IResult<TRequestContext>
    {
        if (task is null)
        {
            throw new InvalidOperationException("The IResult in Task<IResult> response must not be null.");
        }
    }

    private static void EnsureRequestTaskNotNull(Task? task)
    {
        if (task is null)
        {
            throw new InvalidOperationException("The Task returned by the Delegate must not be null.");
        }
    }

    private static IResult<TRequestContext> EnsureRequestResultNotNull(IResult<TRequestContext>? result)
    {
        if (result is null)
        {
            throw new InvalidOperationException("The IResult returned by the Delegate must not be null.");
        }

        return result;
    }

    private static string BuildErrorMessageForMultipleBodyParameters(RequestDelegateFactoryContext<TRequestContext> factoryContext)
    {
        var errorMessage = new StringBuilder();
        errorMessage.AppendLine("Failure to infer one or more parameters.");
        errorMessage.AppendLine("Below is the list of parameters that we found: ");
        errorMessage.AppendLine();
        errorMessage.AppendLine(FormattableString.Invariant($"{"Parameter",-20}| {"Source",-30}"));
        errorMessage.AppendLine("---------------------------------------------------------------------------------");

        FormatTrackedParameters(factoryContext, errorMessage);

        errorMessage.AppendLine().AppendLine();
        errorMessage.AppendLine("Did you mean to register the \"UNKNOWN\" parameters as a Service?")
            .AppendLine();
        return errorMessage.ToString();
    }

    private static void FormatTrackedParameters(RequestDelegateFactoryContext<TRequestContext> factoryContext, StringBuilder errorMessage)
    {
        foreach (var kv in factoryContext.TrackedParameters)
        {
            errorMessage.AppendLine(FormattableString.Invariant($"{kv.Key,-19} | {kv.Value,-15}"));
        }
    }

    private sealed class RdfEndpointBuilder : EndpointBuilder<TRequestContext>
    {
        public RdfEndpointBuilder(IServiceProvider applicationServices)
        {
            ApplicationServices = applicationServices;
        }

        public override Endpoint<TRequestContext> Build()
        {
            throw new NotSupportedException();
        }
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new EmptyServiceProvider();
        public object? GetService(Type serviceType) => null;
    }
}