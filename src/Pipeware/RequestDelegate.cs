// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is licensed under the terms of the MIT license.

namespace Pipeware;

public delegate Task RequestDelegate<TRequestContext>(TRequestContext context) where TRequestContext : IRequestContext;
