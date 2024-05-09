// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing.Abstractions/src/IOutboundParameterTransformer.cs
// Source Sha256: 308c1f7e116e2caa1a30ee2fb707e24c25f1ab7d

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Routing;

/// <summary>
/// Defines the contract that a class must implement to transform route values while building
/// a URI.
/// </summary>
public interface IOutboundParameterTransformer : IParameterPolicy
{
    /// <summary>
    /// Transforms the specified route value to a string for inclusion in a URI.
    /// </summary>
    /// <param name="value">The route value to transform.</param>
    /// <returns>The transformed value.</returns>
    string? TransformOutbound(object? value);
}