// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware.Metadata;
using Pipeware.Routing.Patterns;

namespace Pipeware.Routing;

public sealed partial class RouteEndpointBuilder<TRequestContext>
{
    private static EndpointMetadataCollection CreateMetadataCollection(IList<object> metadata, RoutePattern routePattern)
    {
        var hasRouteDiagnosticsMetadata = false;

        if (metadata.Count > 0)
        {
            for (var i = 0; i < metadata.Count; i++)
            {
                // Not using else if since a metadata could have both
                // interfaces.

                if (!hasRouteDiagnosticsMetadata && metadata[i] is IRouteDiagnosticsMetadata)
                {
                    hasRouteDiagnosticsMetadata = true;
                }
            }
        }

        // No route diagnostics metadata provided so automatically add one based on the route pattern string.
        if (!hasRouteDiagnosticsMetadata)
        {
            metadata.Add(new RouteDiagnosticsMetadata(routePattern.DebuggerToString()));
        }

        return new EndpointMetadataCollection(metadata);
    }
}

