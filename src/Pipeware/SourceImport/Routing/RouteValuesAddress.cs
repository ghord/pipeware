// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/RouteValuesAddress.cs
// Source Sha256: 111eed4acbcf40620409254d345f46477bab30d1

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Linq;
namespace Pipeware.Routing;

/// <summary>
/// An address of route name and values.
/// </summary>
public class RouteValuesAddress
{
    private string? _toString;
    /// <summary>
    /// Gets or sets the route name.
    /// </summary>
    public string? RouteName { get; set; }

    /// <summary>
    /// Gets or sets the route values that are explicitly specified.
    /// </summary>
    public RouteValueDictionary ExplicitValues { get; set; } = default!;

    /// <summary>
    /// Gets or sets ambient route values from the current HTTP request.
    /// </summary>
    public RouteValueDictionary? AmbientValues { get; set; }

    /// <inheritdoc />
    public override string? ToString()
    {
        _toString ??= $"{RouteName}({string.Join(',', ExplicitValues.Select(kv => $"{kv.Key}=[{kv.Value}]"))})";
        return _toString;
    }
}