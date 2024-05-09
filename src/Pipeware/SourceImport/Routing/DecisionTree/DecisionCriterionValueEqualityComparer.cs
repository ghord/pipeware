// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/DecisionTree/DecisionCriterionValueEqualityComparer.cs
// Source Sha256: 6e0ed03dc9ae84b62d4c629f643d1bebfb91f816

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Routing.DecisionTree;

internal sealed class DecisionCriterionValueEqualityComparer : IEqualityComparer<DecisionCriterionValue>
{
    public DecisionCriterionValueEqualityComparer(IEqualityComparer<object> innerComparer)
    {
        InnerComparer = innerComparer;
    }

    public IEqualityComparer<object> InnerComparer { get; private set; }

    public bool Equals(DecisionCriterionValue x, DecisionCriterionValue y)
    {
        return InnerComparer.Equals(x.Value, y.Value);
    }

    public int GetHashCode(DecisionCriterionValue obj)
    {
        return InnerComparer.GetHashCode(obj.Value);
    }
}