// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Shared/RouteValueDictionaryTrimmerWarning.cs
// Source Sha256: 67b7d866b00623ffed357c2cb3de2c554dbed885

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware.Internal;

internal static class RouteValueDictionaryTrimmerWarning
{
    public const string Warning = "This API may perform reflection on supplied parameters which may be trimmed if not referenced directly. " +
        "Initialize a RouteValueDictionary with route values to avoid this issue.";
}