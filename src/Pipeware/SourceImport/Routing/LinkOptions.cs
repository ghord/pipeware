// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing.Abstractions/src/LinkOptions.cs
// Source Sha256: 83d5ec806284ecd6f3e171067a9208b6f4cb7717

// Originally licensed under:

﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Routing;

/// <summary>
/// Configures options for generated URLs.
/// </summary>
public class LinkOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether all generated paths URLs are lowercase.
    /// Use <see cref="LowercaseQueryStrings" /> to configure the behavior for query strings.
    /// </summary>
    public bool? LowercaseUrls { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a generated query strings are lowercase.
    /// This property will be false unless <see cref="LowercaseUrls" /> is also <c>true</c>.
    /// </summary>
    public bool? LowercaseQueryStrings { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a trailing slash should be appended to the generated URLs.
    /// </summary>
    public bool? AppendTrailingSlash { get; set; }
}