// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is licensed under the terms of the MIT license.

using Pipeware.Features;

namespace Pipeware;

public static class RequestContextFeatureExtensions
{
    public static IQueryFeature GetQueryFeature(this IRequestContext requestContext)
    {
        return requestContext.Features.Get<IQueryFeature>() ?? throw new NotSupportedException("IQueryFeature not found on requestContext");
    }

    public static IRequestPathFeature GetRequestPathFeature(this IRequestContext requestContext)
    {
        return requestContext.Features.Get<IRequestPathFeature>() ?? throw new NotSupportedException("IRequestPathFeature not found on requestContext");
    }

    public static IRouteValuesFeature GetRouteValuesFeature(this IRequestContext requestContext)
    {
        return requestContext.Features.Get<IRouteValuesFeature>() ?? throw new NotSupportedException("IRouteValuesFeature not found on requestContext");
    }

    public static IResultFailureFeature GetResultFailureFeature(this IRequestContext requestContext)
    {
        return requestContext.Features.Get<IResultFailureFeature>() ?? throw new NotSupportedException("IResultFailureFeature not found on requestContext");
    }

    public static IResultObjectFeature GetResultObjectFeature(this IRequestContext requestContext)
    {
        return requestContext.Features.Get<IResultObjectFeature>() ?? throw new NotSupportedException("IResultObjectFeature not found on requestContext");
    }

    public static IRequestLifetimeFeature GetRequestLifetimeFeature(this IRequestContext requestContext)
    {
        return requestContext.Features.Get<IRequestLifetimeFeature>() ?? throw new NotSupportedException("IRequestLifetimeFeature not found on requestContext");
    }
}
