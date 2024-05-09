// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Http.Abstractions/src/AsParametersAttribute.cs
// Source Sha256: 5ed7ac1b1abaf336c9818b96c53a0e6fb581d6ab

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware;

using System;

/// <summary>
/// Specifies that a route handler delegate's parameter represents a structured parameter list.
/// </summary>
[AttributeUsage(
    AttributeTargets.Parameter,
    Inherited = false,
    AllowMultiple = false)]
public sealed class AsParametersAttribute : Attribute
{
}