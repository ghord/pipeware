// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/DefaultInlineConstraintResolver.cs
// Source Sha256: bddbe2c912abee1e3c1212d39d9fb09c7b94c927

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.Extensions.Options;

namespace Pipeware.Routing;

#if !COMPONENTS
/// <summary>
/// The default implementation of <see cref="IInlineConstraintResolver"/>. Resolves constraints by parsing
/// a constraint key and constraint arguments, using a map to resolve the constraint type, and calling an
/// appropriate constructor for the constraint type.
/// </summary>
public class DefaultInlineConstraintResolver<TRequestContext> : IInlineConstraintResolver
, IInlineConstraintResolver<TRequestContext> where TRequestContext : class, IRequestContext
#else
internal class DefaultInlineConstraintResolver : IInlineConstraintResolver
#endif
{
    private readonly IDictionary<string, Type> _inlineConstraintMap;
    private readonly IServiceProvider _serviceProvider;

#if !COMPONENTS
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultInlineConstraintResolver{TRequestContext}"/> class.
    /// </summary>
    /// <param name="routeOptions">Accessor for <see cref="RouteOptions{TRequestContext}"/> containing the constraints of interest.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to get service arguments from.</param>
#endif
    public DefaultInlineConstraintResolver(IOptions<RouteOptions<TRequestContext>> routeOptions, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(routeOptions);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _inlineConstraintMap = routeOptions.Value.TrimmerSafeConstraintMap;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    /// <example>
    /// A typical constraint looks like the following
    /// "exampleConstraint(arg1, arg2, 12)".
    /// Here if the type registered for exampleConstraint has a single constructor with one argument,
    /// The entire string "arg1, arg2, 12" will be treated as a single argument.
    /// In all other cases arguments are split at comma.
    /// </example>
    public virtual IRouteConstraint? ResolveConstraint(string inlineConstraint)
    {
        ArgumentNullException.ThrowIfNull(inlineConstraint);

        // This will return null if the text resolves to a non-IRouteConstraint
        return ParameterPolicyActivator.ResolveParameterPolicy<IRouteConstraint>(
            _inlineConstraintMap,
            _serviceProvider,
            inlineConstraint,
            out _);
    }
}