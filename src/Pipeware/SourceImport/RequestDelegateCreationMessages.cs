// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is derivative work of aspnetcore project and is licensed under the terms of the MIT license.

// Source file: https://github.com/dotnet/aspnetcore/tree/release/8.0/src/Shared/RequestDelegateCreationMessages.cs
// Source Sha256: d36975c5b45250821ba6a921f38a43432f40984d

// Originally licensed under:

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Pipeware;

internal static partial class RequestDelegateCreationLogging
{

    public const int ParameterBindingFailedEventId = 3;
    public const string ParameterBindingFailedEventName = "ParameterBindingFailed";
    public const string ParameterBindingFailedLogMessage = @"Failed to bind parameter ""{ParameterType} {ParameterName}"" from ""{SourceValue}"".";
    public const string ParameterBindingFailedExceptionMessage = @"Failed to bind parameter ""{0} {1}"" from ""{2}"".";

    public const int RequiredParameterNotProvidedEventId = 4;
    public const string RequiredParameterNotProvidedEventName = "RequiredParameterNotProvided";
    public const string RequiredParameterNotProvidedLogMessage = @"Required parameter ""{ParameterType} {ParameterName}"" was not provided from {Source}.";
    public const string RequiredParameterNotProvidedExceptionMessage = @"Required parameter ""{0} {1}"" was not provided from {2}.";

    public const int ImplicitBodyNotProvidedEventId = 5;
    public const string ImplicitBodyNotProvidedEventName = "ImplicitBodyNotProvided";
    public const string ImplicitBodyNotProvidedLogMessage = @"Implicit body inferred for parameter ""{ParameterName}"" but no body was provided. Did you mean to use a Service instead?";
    public const string ImplicitBodyNotProvidedExceptionMessage = @"Implicit body inferred for parameter ""{0}"" but no body was provided. Did you mean to use a Service instead?";
}