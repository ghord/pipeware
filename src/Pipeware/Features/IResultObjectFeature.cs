// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is licensed under the terms of the MIT license.

namespace Pipeware.Features;

public interface IResultObjectFeature
{
    object? Result { get; }

    Task SetResultAsync(object? result);
}
