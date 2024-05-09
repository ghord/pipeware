// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/MatcherBuilder.cs
// Source Sha256: 77771e2a4e99037b8fb1c8be6f63e1fda3755f89

// Originally licensed under:

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Routing.Matching;

internal abstract class MatcherBuilder<TRequestContext> where TRequestContext : class, IRequestContext
{
    public abstract void AddEndpoint(RouteEndpoint<TRequestContext> endpoint);

    public abstract Matcher<TRequestContext> Build();
}