// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/UriBuilderContextPooledObjectPolicy.cs
// Source Sha256: 19a368cf336232a7916d72dc13866b128c865938

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.Extensions.ObjectPool;

namespace Pipeware.Routing;

internal sealed class UriBuilderContextPooledObjectPolicy : IPooledObjectPolicy<UriBuildingContext>
{
    public UriBuildingContext Create()
    {
        return new UriBuildingContext(UrlEncoder.Default);
    }

    public bool Return(UriBuildingContext obj)
    {
        obj.Clear();
        return true;
    }
}