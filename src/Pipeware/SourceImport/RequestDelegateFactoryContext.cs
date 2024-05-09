// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Extensions/src/RequestDelegateFactoryContext.cs
// Source Sha256: ff3010a68131bd697e81edddc1f4ece3a806641f

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using Pipeware.Builder;

using Microsoft.Extensions.DependencyInjection;

namespace Pipeware;

internal sealed class RequestDelegateFactoryContext<TRequestContext> where TRequestContext : class, IRequestContext
{
    // Options
    public required IServiceProvider ServiceProvider { get; init; }
    public required IServiceProviderIsService? ServiceProviderIsService { get; init; }
    public required IEnumerable<string>? RouteParameters { get; init; }
    public required bool ThrowOnBadRequest { get; init; }
    public required EndpointBuilder<TRequestContext> EndpointBuilder { get; init; }

    // Handler could be null if the MethodInfo overload of RDF.Create is used, but that doesn't matter because this is
    // only referenced to optimize certain cases where a RequestDelegate is the handler and filters don't modify it.
    public Delegate? Handler { get; set; }

    // Temporary State

    // This indicates whether we're currently in RDF.Create() with a non-null RequestDelegateResult.
    // This is settable, because if this context is cached we need to set it to true after it's created.
    // But it's still possible this should be initialized to true initially, so we're making it required.
    // In theory, someone could construct their own RequestDelegateResult without a cached context.
    public required bool MetadataAlreadyInferred { get; set; }

    public ParameterInfo? RequestBodyParameter { get; set; }
    public bool AllowEmptyRequestBody { get; set; }

    public bool UsingTempSourceString { get; set; }
    public List<ParameterExpression> ExtraLocals { get; } = new();
    public List<Expression> ParamCheckExpressions { get; } = new();
    public List<Func<TRequestContext, ValueTask<object?>>> ParameterBinders { get; } = new();

    public Dictionary<string, string> TrackedParameters { get; } = new();
    public bool HasMultipleBodyParameters { get; set; }
    public bool HasInferredBody { get; set; }

    public NullabilityInfoContext NullabilityContext { get; } = new();

    // Used to invoke TryResolveFormAsync once per handler so that we can
    // avoid the blocking code-path that occurs when `httpContext.Request.Form`
    // is called.
    public bool ReadForm { get; set; }
    public bool ReadFormFile { get; set; }
    public ParameterInfo? FirstFormRequestBodyParameter { get; set; }
    // Properties for constructing and managing filters
    public Expression? MethodCall { get; set; }
    public Type[] ArgumentTypes { get; set; } = Array.Empty<Type>();
    public Expression[]? ArgumentExpressions { get; set; }
    public Expression[] BoxedArgs { get; set; } = Array.Empty<Expression>();
    public bool FilterFactoriesHaveRunWithoutModifyingPerRequestBehavior { get; set; }

    public List<ParameterInfo> Parameters { get; set; } = new();
}