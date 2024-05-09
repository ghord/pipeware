// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: src/Shared/RouteHandlers/ExecuteHandlerHelper.cs

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Pipeware.Internal;

internal static class ExecuteHandlerHelper
{
    public static Task ExecuteReturnAsync<TRequestContext>(object? obj, TRequestContext requestContext) where TRequestContext : class, IRequestContext
    {
        // Terminal built ins
        if (obj is IResult<TRequestContext> result)
        {
            return result.ExecuteAsync(requestContext);
        }
        else
        {
            var feature = requestContext.GetResultObjectFeature();

            return feature.SetResultAsync(obj);
        }
    }
}