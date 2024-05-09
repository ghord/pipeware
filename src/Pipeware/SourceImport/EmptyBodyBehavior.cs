// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Mvc/Mvc.Abstractions/src/ModelBinding/EmptyBodyBehavior.cs
// Source Sha256: 158636d75df0a418fcb7aa3d99b86bced7d753aa

// Originally licensed under:

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware;

/// <summary>
/// Determines the behavior for processing empty bodies during input formatting.
/// </summary>
public enum EmptyBodyBehavior
{
    /// <summary>
    /// Uses the framework default behavior for processing empty bodies.
    /// This is typically configured using <c>MvcOptions.AllowEmptyInputInBodyModelBinding</c>.
    /// </summary>
    Default,

    /// <summary>
    /// Empty bodies are treated as valid inputs.
    /// </summary>
    Allow,

    /// <summary>
    /// Empty bodies are treated as invalid inputs.
    /// </summary>
    Disallow,
}