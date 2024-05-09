// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/Matching/MatcherFactory.cs
// Source Sha256: 1ac712fcc25ee3bd44bc6cddc92decd5ab53782f

// Originally licensed under:

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Routing.Matching;

internal abstract class MatcherFactory<TRequestContext> where TRequestContext : class, IRequestContext
{
    public abstract Matcher<TRequestContext> CreateMatcher(EndpointDataSource<TRequestContext> dataSource);
}