// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Shared/RoslynUtils/TypeHelper.cs
// Source Sha256: 51ca29d9c925cb47b4f0e155cc62170122c8c226

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Pipeware.Internal;

internal static class TypeHelper
{
    /// <summary>
    /// Checks to see if a given type is compiler generated.
    /// <remarks>
    /// The compiler will annotate either the target type or the declaring type
    /// with the CompilerGenerated attribute. We walk up the declaring types until
    /// we find a CompilerGenerated attribute or declare the type as not compiler
    /// generated otherwise.
    /// </remarks>
    /// </summary>
    /// <param name="type">The type to evaluate.</param>
    /// <returns><see langword="true" /> if <paramref name="type"/> is compiler generated.</returns>
    internal static bool IsCompilerGeneratedType(Type? type = null)
    {
        if (type is not null)
        {
            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute)) || IsCompilerGeneratedType(type.DeclaringType);
        }
        return false;
    }

    /// <summary>
    /// Checks to see if a given method is compiler generated.
    /// </summary>
    /// <param name="method">The method to evaluate.</param>
    /// <returns><see langword="true" /> if <paramref name="method"/> is compiler generated.</returns>
    private static bool IsCompilerGeneratedMethod(MethodInfo method)
    {
        return Attribute.IsDefined(method, typeof(CompilerGeneratedAttribute)) || IsCompilerGeneratedType(method.DeclaringType);
    }

    /// <summary>
    /// Parses generated local function name out of a generated method name. This code is a stop-gap and exists to address the issues with extracting
    /// original method names from generated local functions. See https://github.com/dotnet/roslyn/issues/55651 for more info.
    /// </summary>
    private static bool TryParseLocalFunctionName(string generatedName, [NotNullWhen(true)] out string? originalName)
    {
        originalName = null;

        var startIndex = generatedName.LastIndexOf(">g__", StringComparison.Ordinal);
        var endIndex = generatedName.LastIndexOf("|", StringComparison.Ordinal);
        if (startIndex >= 0 && endIndex >= 0 && endIndex - startIndex > 4)
        {
            originalName = generatedName.Substring(startIndex + 4, endIndex - startIndex - 4);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Tries to get non-compiler-generated name of function. This parses generated local function names out of a generated method name if possible.
    /// </summary>
    internal static bool TryGetNonCompilerGeneratedMethodName(MethodInfo method, [NotNullWhen(true)] out string? originalName)
    {
        var methodName = method.Name;

        if (!IsCompilerGeneratedMethod(method))
        {
            originalName = methodName;
            return true;
        }

        return TryParseLocalFunctionName(methodName, out originalName);
    }
}
