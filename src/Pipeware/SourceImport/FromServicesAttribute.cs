// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Mvc/Mvc.Core/src/FromServicesAttribute.cs
// Source Sha256: 77ecb5b993d731b38259aa8b39e16990bd769876

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware.Metadata;
using Pipeware;

namespace Pipeware;

/// <summary>
/// Specifies that a parameter or property should be bound using the request services.
/// </summary>
/// <example>
/// In this example an implementation of IProductModelRequestService is registered as a service.
/// Then in the GetProduct action, the parameter is bound to an instance of IProductModelRequestService
/// which is resolved from the request services.
///
/// <code>
/// [HttpGet]
/// public ProductModel GetProduct([FromServices] IProductModelRequestService productModelRequest)
/// {
///     return productModelRequest.Value;
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class FromServicesAttribute : Attribute, IFromServiceMetadata
{
}