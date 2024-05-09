// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/IDynamicEndpointMetadata.cs
// Source Sha256: 501fe5b2eff5f3f7e042250ae3670da2dd239796

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Pipeware;
using Pipeware.Routing.Matching;

namespace Pipeware.Routing;

/// <summary>
/// A metadata interface that can be used to specify that the associated <see cref="Endpoint{TRequestContext}" />
/// will be dynamically replaced during matching.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IDynamicEndpointMetadata"/> and related derived interfaces signal to
/// <see cref="MatcherPolicy{TRequestContext}"/> implementations that an <see cref="Endpoint{TRequestContext}"/> has dynamic behavior
/// and thus cannot have its characteristics cached.
/// </para>
/// <para>
/// Using dynamic endpoints can be useful because the default matcher implementation does not
/// supply extensibility for how URLs are processed. Routing implementations that have dynamic
/// behavior can apply their dynamic logic after URL processing, by replacing a endpoints as
/// part of a <see cref="CandidateSet{TRequestContext}"/>.
/// </para>
/// </remarks>
public interface IDynamicEndpointMetadata
{
    /// <summary>
    /// Returns a value that indicates whether the associated endpoint has dynamic matching
    /// behavior.
    /// </summary>
    bool IsDynamic { get; }
}