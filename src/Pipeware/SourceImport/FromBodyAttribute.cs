// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Mvc/Mvc.Core/src/FromBodyAttribute.cs
// Source Sha256: 411240dae9ba43e763efe869e367b1749c5311f6

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware.Metadata;
using Pipeware;

namespace Pipeware;

/// <summary>
/// Specifies that a parameter or property should be bound using the request body.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class FromBodyAttribute : Attribute, IFromBodyMetadata
{

    /// <summary>
    /// Gets or sets a value which decides whether body model binding should treat empty
    /// input as valid.
    /// </summary>
    /// <remarks>
    /// The default behavior is to use framework defaults as configured by <see cref="MvcOptions.AllowEmptyInputInBodyModelBinding"/>.
    /// Specifying <see cref="EmptyBodyBehavior.Allow"/> or <see cref="EmptyBodyBehavior.Disallow" /> will override the framework defaults.
    /// </remarks>
    public EmptyBodyBehavior EmptyBodyBehavior { get; set; }

    // Since the default behavior is to reject empty bodies if MvcOptions.AllowEmptyInputInBodyModelBinding is not configured,
    // we'll consider EmptyBodyBehavior.Default the same as EmptyBodyBehavior.Disallow.
    bool IFromBodyMetadata.AllowEmpty => EmptyBodyBehavior == EmptyBodyBehavior.Allow;
}