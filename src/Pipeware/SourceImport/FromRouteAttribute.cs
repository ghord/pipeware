// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Mvc/Mvc.Core/src/FromRouteAttribute.cs
// Source Sha256: e3fee8160c31bb67e4bf5605f85ff88e3508a789

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;
using Pipeware.Metadata;


namespace Pipeware;

/// <summary>
/// Specifies that a parameter or property should be bound using route-data from the current request.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class FromRouteAttribute : Attribute, IFromRouteMetadata
{

    /// <summary>
    /// The <see cref="HttpRequest.RouteValues"/> name.
    /// </summary>
    public string? Name { get; set; }
}