// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/DefaultParameterPolicyFactory.cs
// Source Sha256: 8f7cf63f24a95508e96b0bb456f8d6239ec93de1

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware.Routing.Constraints;
using Pipeware.Routing.Patterns;
using Microsoft.Extensions.Options;

namespace Pipeware.Routing;

internal sealed class DefaultParameterPolicyFactory<TRequestContext> : ParameterPolicyFactory<TRequestContext> where TRequestContext : class, IRequestContext
{
    private readonly RouteOptions<TRequestContext> _options;
    private readonly IServiceProvider _serviceProvider;

    public DefaultParameterPolicyFactory(
        IOptions<RouteOptions<TRequestContext>> options,
        IServiceProvider serviceProvider)
    {
        _options = options.Value;
        _serviceProvider = serviceProvider;
    }

    public override IParameterPolicy Create(RoutePatternParameterPart? parameter, IParameterPolicy parameterPolicy)
    {
        ArgumentNullException.ThrowIfNull(parameterPolicy);

        if (parameterPolicy is IRouteConstraint routeConstraint)
        {
            return InitializeRouteConstraint(parameter?.IsOptional ?? false, routeConstraint);
        }

        return parameterPolicy;
    }

    public override IParameterPolicy Create(RoutePatternParameterPart? parameter, string inlineText)
    {
        ArgumentNullException.ThrowIfNull(inlineText);

        var parameterPolicy = ParameterPolicyActivator.ResolveParameterPolicy<IParameterPolicy>(
            _options.TrimmerSafeConstraintMap,
            _serviceProvider,
            inlineText,
            out var parameterPolicyKey);

        if (parameterPolicy == null)
        {
            throw new InvalidOperationException(string.Format("The constraint reference '{0}' could not be resolved to a type. Register the constraint type with '{1}.{2}'.", parameterPolicyKey, typeof(RouteOptions<TRequestContext>), nameof(RouteOptions<TRequestContext>.ConstraintMap)));
        }

        if (parameterPolicy is IRouteConstraint constraint)
        {
            return InitializeRouteConstraint(parameter?.IsOptional ?? false, constraint);
        }

        return parameterPolicy;
    }

    private static IParameterPolicy InitializeRouteConstraint(
        bool optional,
        IRouteConstraint routeConstraint)
    {
        if (optional)
        {
            routeConstraint = new OptionalRouteConstraint(routeConstraint);
        }

        return routeConstraint;
    }
}