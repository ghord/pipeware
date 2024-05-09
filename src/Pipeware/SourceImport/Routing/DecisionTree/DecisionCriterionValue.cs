// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/DecisionTree/DecisionCriterionValue.cs
// Source Sha256: 52061cd760039b95d634298c9b2ea398cf831fd0

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Routing.DecisionTree;

internal readonly struct DecisionCriterionValue
{
    private readonly object _value;

    public DecisionCriterionValue(object value)
    {
        _value = value;
    }

    public object Value
    {
        get { return _value; }
    }
}