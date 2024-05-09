// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/DecisionTree/ItemDescriptor.cs
// Source Sha256: 675ca3688bd2ad564aa91cc52c92c1855f960e2a

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace Pipeware.Routing.DecisionTree;

internal sealed class ItemDescriptor<TItem>
{
    public IDictionary<string, DecisionCriterionValue> Criteria { get; set; }

    public int Index { get; set; }

    public TItem Item { get; set; }
}