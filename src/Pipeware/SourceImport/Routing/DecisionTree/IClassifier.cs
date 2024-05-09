// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/DecisionTree/IClassifier.cs
// Source Sha256: 6f0f647cc79f8eea162321ed846f8bbaaf4cf5b1

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Routing.DecisionTree;

internal interface IClassifier<TItem>
{
    IDictionary<string, DecisionCriterionValue> GetCriteria(TItem item);

    IEqualityComparer<object> ValueComparer { get; }
}