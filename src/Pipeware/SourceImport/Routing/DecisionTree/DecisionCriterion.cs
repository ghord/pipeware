// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Http/Routing/src/DecisionTree/DecisionCriterion.cs
// Source Sha256: 0c4cbcb1f67901003bfd3967dfac9889b0a5ae5c

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

namespace Pipeware.Routing.DecisionTree;

internal sealed class DecisionCriterion<TItem>
{
    public string Key { get; set; }

    public Dictionary<object, DecisionTreeNode<TItem>> Branches { get; set; }
}