// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is licensed under the terms of the MIT license.

using Pipeware.Features;

namespace Pipeware;

public interface IRequestContext
{
    IFeatureCollection Features { get; }

    IDictionary<string, object?> Items { get; }

    IServiceProvider RequestServices { get; }
}