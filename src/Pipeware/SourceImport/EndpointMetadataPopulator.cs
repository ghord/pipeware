// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Shared/EndpointMetadataPopulator.cs
// Source Sha256: c17dbf818d595d15cef211e49dd5126b8ed7a11c

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Pipeware.Builder;
using Pipeware.Metadata;
using Pipeware.Internal;

#nullable enable

namespace Pipeware;

[RequiresUnreferencedCode("This API performs reflection on types that can't be statically analyzed.")]
[RequiresDynamicCode("This API performs reflection on types that can't be statically analyzed.")]
internal static class EndpointMetadataPopulator<TRequestContext> where TRequestContext : class, IRequestContext
{
    private static readonly MethodInfo PopulateMetadataForParameterMethod = typeof(EndpointMetadataPopulator<TRequestContext>).GetMethod(nameof(PopulateMetadataForParameter), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo PopulateMetadataForEndpointMethod = typeof(EndpointMetadataPopulator<TRequestContext>).GetMethod(nameof(PopulateMetadataForEndpoint), BindingFlags.NonPublic | BindingFlags.Static)!;

    public static void PopulateMetadata(MethodInfo methodInfo, EndpointBuilder<TRequestContext> builder, IEnumerable<ParameterInfo>? parameters = null)
    {
        object?[]? invokeArgs = null;
        parameters ??= methodInfo.GetParameters();

        // Get metadata from parameter types
        foreach (var parameter in parameters)
        {
            if (typeof(IEndpointParameterMetadataProvider<TRequestContext>).IsAssignableFrom(parameter.ParameterType))
            {
                // Parameter type implements IEndpointParameterMetadataProvider
                invokeArgs ??= new object[2];
                invokeArgs[0] = parameter;
                invokeArgs[1] = builder;
                PopulateMetadataForParameterMethod.MakeGenericMethod(parameter.ParameterType).Invoke(null, invokeArgs);
            }

            if (typeof(IEndpointMetadataProvider<TRequestContext>).IsAssignableFrom(parameter.ParameterType))
            {
                // Parameter type implements IEndpointMetadataProvider
                invokeArgs ??= new object[2];
                invokeArgs[0] = methodInfo;
                invokeArgs[1] = builder;
                PopulateMetadataForEndpointMethod.MakeGenericMethod(parameter.ParameterType).Invoke(null, invokeArgs);
            }
        }

        // Get metadata from return type
        var returnType = methodInfo.ReturnType;
        if (AwaitableInfo.IsTypeAwaitable(returnType, out var awaitableInfo))
        {
            returnType = awaitableInfo.ResultType;
        }

        if (returnType is not null && typeof(IEndpointMetadataProvider<TRequestContext>).IsAssignableFrom(returnType))
        {
            // Return type implements IEndpointMetadataProvider
            invokeArgs ??= new object[2];
            invokeArgs[0] = methodInfo;
            invokeArgs[1] = builder;
            PopulateMetadataForEndpointMethod.MakeGenericMethod(returnType).Invoke(null, invokeArgs);
        }
    }

    private static void PopulateMetadataForParameter<T>(ParameterInfo parameter, EndpointBuilder<TRequestContext> builder)
        where T : IEndpointParameterMetadataProvider<TRequestContext>
    {
        T.PopulateMetadata(parameter, builder);
    }

    private static void PopulateMetadataForEndpoint<T>(MethodInfo method, EndpointBuilder<TRequestContext> builder)
        where T : IEndpointMetadataProvider<TRequestContext>
    {
        T.PopulateMetadata(method, builder);
    }
}