// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Mvc/Mvc.Core/src/FromQueryAttribute.cs
// Source Sha256: 3f9727f344b41b9a4434585ed51544dc9f1e3a07

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware.Metadata;
using Pipeware;

namespace Pipeware;

/// <summary>
/// Specifies that a parameter or property should be bound using the request query string.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class FromQueryAttribute : Attribute, IFromQueryMetadata
{

    /// <inheritdoc />
    public string? Name { get; set; }
}