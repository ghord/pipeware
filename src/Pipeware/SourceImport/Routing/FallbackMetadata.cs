// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/FallbackMetadata.cs
// Source Sha256: eadf927e609c475b473b05d60b0fef3568a9e9d3

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Routing;

internal sealed class FallbackMetadata
{
    public static readonly FallbackMetadata Instance = new FallbackMetadata();

    private FallbackMetadata()
    {
    }
}