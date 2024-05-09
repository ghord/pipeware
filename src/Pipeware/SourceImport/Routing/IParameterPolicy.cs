// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing.Abstractions/src/IParameterPolicy.cs
// Source Sha256: 44e5e7d3a1a69800dcd14a59f66d7900d8056459

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Routing;

#if !COMPONENTS
/// <summary>
/// A marker interface for types that are associated with route parameters.
/// </summary>
public interface IParameterPolicy
#else
internal interface IParameterPolicy
#endif
{
}