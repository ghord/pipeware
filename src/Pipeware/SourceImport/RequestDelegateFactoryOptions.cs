// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Extensions/src/RequestDelegateFactoryOptions.cs
// Source Sha256: a11cba4243305696366bd71016dc3ada68d07aa7

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware.Builder;
using Pipeware.Metadata;
using Microsoft.Extensions.Logging;

namespace Pipeware;

/// <summary>
/// Options for controlling the behavior of the <see cref="RequestDelegate{TRequestContext}" /> when created using <see cref="RequestDelegateFactory{TRequestContext}" />.
/// </summary>
public sealed class RequestDelegateFactoryOptions<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// The <see cref="IServiceProvider"/> instance used to access application services.
    /// </summary>
    public IServiceProvider? ServiceProvider { get; init; }

    /// <summary>
    /// The list of route parameter names that are specified for this handler.
    /// </summary>
    public IEnumerable<string>? RouteParameterNames { get; init; }

    /// <summary>
    /// Controls whether the <see cref="RequestDelegate{TRequestContext}"/> should throw a <see cref="BadHttpRequestException"/> in addition to
    /// writing a <see cref="LogLevel.Debug"/> log when handling invalid requests.
    /// </summary>
    public bool ThrowOnBadRequest { get; init; }

    /// <summary>
    /// The mutable <see cref="Builder.EndpointBuilder{TRequestContext}"/> used to assist in the creation of the <see cref="RequestDelegateResult.RequestDelegate{TRequestContext}"/>.
    /// This is primarily used to run <see cref="EndpointBuilder.FilterFactories"/> and populate inferred <see cref="EndpointBuilder.Metadata"/>.
    /// The <see cref="EndpointBuilder.RequestDelegate{TRequestContext}"/> must be <see langword="null"/>. After the call to <see cref="RequestDelegateFactory.Create(Delegate, RequestDelegateFactoryOptions<TRequestContext>?)"/>,
    /// <see cref="EndpointBuilder.RequestDelegate{TRequestContext}"/> will be the same as <see cref="RequestDelegateResult.RequestDelegate{TRequestContext}"/>.
    /// </summary>
    /// <remarks>
    /// Any metadata already in <see cref="EndpointBuilder.Metadata"/> will be included in <see cref="RequestDelegateResult.EndpointMetadata" /> <b>before</b>
    /// most metadata inferred during creation of the <see cref="RequestDelegateResult.RequestDelegate{TRequestContext}"/> and <b>before</b> any metadata provided by types in
    /// the delegate signature that implement <see cref="IEndpointMetadataProvider{TRequestContext}" /> or <see cref="IEndpointParameterMetadataProvider{TRequestContext}" />. The exception to this general rule is the
    /// <see cref="IAcceptsMetadata"/> that <see cref="RequestDelegateFactory.Create(Delegate, RequestDelegateFactoryOptions<TRequestContext>?)"/> infers automatically
    /// without any custom metadata providers which instead is inserted at the start to give it lower precedence. Custom metadata providers can choose to
    /// insert their metadata at the start to give lower precedence, but this is unusual.
    /// </remarks>
    public EndpointBuilder<TRequestContext>? EndpointBuilder { get; init; }
}