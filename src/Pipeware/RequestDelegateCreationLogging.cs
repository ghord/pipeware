// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is licensed under the terms of the MIT license.

namespace Pipeware;

internal static partial class RequestDelegateCreationLogging
{
    public const int InvalidRequestBodyTypeEventId = 1;
    public const string InvalidRequestBodyTypeEventName = "InvalidRequestTypeBody";
    public const string InvalidRequestBodyTypeLogMessage = @"Failed to read parameter ""{ParameterType} {ParameterName}"" from the request body type ""{BodyType}"".";
    public const string InvalidRequestBodyTypeExceptionMessage = @"Failed to read parameter ""{0} {1}"" from the request body type ""{2}"".";

    public const int InvalidRequestBodyEventId = 2;
    public const string InvalidRequestBodyEventName = "InvalidRequestBody";
    public const string InvalidRequestBodyLogMessage = @"Failed to read parameter ""{ParameterType} {ParameterName}"" from the request body.";
    public const string InvalidRequestBodyExceptionMessage = @"Failed to read parameter ""{0} {1}"" from the request body.";
}
