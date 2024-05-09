// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is licensed under the terms of the MIT license.

namespace Pipeware.Features;

public interface IRequestPathFeature
{
    PathString PathBase { get; set; }

    PathString Path { get; set; }

    string QueryString { get; set; }
}