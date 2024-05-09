// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/EndpointDataSource.cs
// Source Sha256: 83b65d054a52603a9ad13a29837e847069374036

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Pipeware;
using Pipeware.Routing.Patterns;
using Microsoft.Extensions.Primitives;

namespace Pipeware.Routing;

/// <summary>
/// Provides a collection of <see cref="Endpoint{TRequestContext}"/> instances.
/// </summary>
public abstract class EndpointDataSource<TRequestContext> where TRequestContext : class, IRequestContext
{
    /// <summary>
    /// Gets a <see cref="IChangeToken"/> used to signal invalidation of cached <see cref="Endpoint{TRequestContext}"/>
    /// instances.
    /// </summary>
    /// <returns>The <see cref="IChangeToken"/>.</returns>
    public abstract IChangeToken GetChangeToken();

    /// <summary>
    /// Returns a read-only collection of <see cref="Endpoint{TRequestContext}"/> instances.
    /// </summary>
    public abstract IReadOnlyList<Endpoint<TRequestContext>> Endpoints { get; }

    /// <summary>
    /// Get the <see cref="Endpoint{TRequestContext}"/> instances for this <see cref="EndpointDataSource{TRequestContext}"/> given the specified <see cref="RouteGroupContext.Prefix"/> and <see cref="RouteGroupContext.Conventions"/>.
    /// </summary>
    /// <param name="context">Details about how the returned <see cref="Endpoint{TRequestContext}"/> instances should be grouped and a reference to application services.</param>
    /// <returns>
    /// Returns a read-only collection of <see cref="Endpoint{TRequestContext}"/> instances given the specified group <see cref="RouteGroupContext.Prefix"/> and <see cref="RouteGroupContext.Conventions"/>.
    /// </returns>
    public virtual IReadOnlyList<Endpoint<TRequestContext>> GetGroupedEndpoints(RouteGroupContext<TRequestContext> context)
    {
        // Only evaluate Endpoints once per call.
        var endpoints = Endpoints;
        var wrappedEndpoints = new RouteEndpoint<TRequestContext>[endpoints.Count];

        for (int i = 0; i < endpoints.Count; i++)
        {
            var endpoint = endpoints[i];

            // Endpoint does not provide a RoutePattern but RouteEndpoint does. So it's impossible to apply a prefix for custom Endpoints.
            // Supporting arbitrary Endpoints just to add group metadata would require changing the Endpoint type breaking any real scenario.
            if (endpoint is not RouteEndpoint<TRequestContext> routeEndpoint)
            {
                throw new NotSupportedException(string.Format("MapGroup does not support custom Endpoint type '{0}'. Only RouteEndpoints can be grouped.", endpoint.GetType()));
            }

            // Make the full route pattern visible to IEndpointConventionBuilder extension methods called on the group.
            // This includes patterns from any parent groups.
            var fullRoutePattern = RoutePatternFactory.Combine(context.Prefix, routeEndpoint.RoutePattern);
            var routeEndpointBuilder = new RouteEndpointBuilder<TRequestContext>(routeEndpoint.RequestDelegate, fullRoutePattern, routeEndpoint.Order)
            {
                DisplayName = routeEndpoint.DisplayName,
                ApplicationServices = context.ApplicationServices,
            };

            // Apply group conventions to each endpoint in the group at a lower precedent than metadata already on the endpoint.
            foreach (var convention in context.Conventions)
            {
                convention(routeEndpointBuilder);
            }

            // Any metadata already on the RouteEndpoint must have been applied directly to the endpoint or to a nested group.
            // This makes the metadata more specific than what's being applied to this group. So add it after this group's conventions.
            foreach (var metadata in routeEndpoint.Metadata)
            {
                routeEndpointBuilder.Metadata.Add(metadata);
            }

            foreach (var finallyConvention in context.FinallyConventions)
            {
                finallyConvention(routeEndpointBuilder);
            }

            // The RoutePattern, RequestDelegate, Order and DisplayName can all be overridden by non-group-aware conventions.
            // Unlike with metadata, if a convention is applied to a group that changes any of these, I would expect these
            // to be overridden as there's no reasonable way to merge these properties.
            wrappedEndpoints[i] = (RouteEndpoint<TRequestContext>)routeEndpointBuilder.Build();
        }

        return wrappedEndpoints;
    }

    // We don't implement DebuggerDisplay directly on the EndpointDataSource base type because this could have side effects.
    internal static string GetDebuggerDisplayStringForEndpoints(IReadOnlyList<Endpoint<TRequestContext>>? endpoints)
    {
        if (endpoints is null || endpoints.Count == 0)
        {
            return "No endpoints";
        }

        var sb = new StringBuilder();

        foreach (var endpoint in endpoints)
        {
            if (endpoint is RouteEndpoint<TRequestContext> routeEndpoint)
            {
                var template = routeEndpoint.RoutePattern.RawText;
                template = string.IsNullOrEmpty(template) ? "\"\"" : template;
                sb.Append(template);
                sb.Append(", Defaults: new { ");
                FormatValues(sb, routeEndpoint.RoutePattern.Defaults);
                sb.Append(" }");
                var routeNameMetadata = routeEndpoint.Metadata.GetMetadata<IRouteNameMetadata>();
                sb.Append(", Route Name: ");
                sb.Append(routeNameMetadata?.RouteName);
                var routeValues = routeEndpoint.RoutePattern.RequiredValues;

                if (routeValues.Count > 0)
                {
                    sb.Append(", Required Values: new { ");
                    FormatValues(sb, routeValues);
                    sb.Append(" }");
                }

                sb.Append(", Order: ");
                sb.Append(routeEndpoint.Order);

                sb.Append(", Display Name: ");
            }
            else
            {
                sb.Append("Non-RouteEndpoint. DisplayName: ");
            }

            sb.AppendLine(endpoint.DisplayName);
        }

        return sb.ToString();

        static void FormatValues(StringBuilder sb, IEnumerable<KeyValuePair<string, object?>> values)
        {
            var isFirst = true;

            foreach (var (key, value) in values)
            {
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    sb.Append(", ");
                }

                sb.Append(key);
                sb.Append(" = ");

                if (value is null)
                {
                    sb.Append("null");
                }
                else
                {
                    sb.Append('\"');
                    sb.Append(value);
                    sb.Append('\"');
                }
            }
        }
    }
}