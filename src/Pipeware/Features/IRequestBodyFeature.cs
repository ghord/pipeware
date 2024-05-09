// Copyright (c) 2024 Grzegorz Hordyński. All rights reserved.
// This file is licensed under the terms of the MIT license.

namespace Pipeware.Features
{
    public interface IRequestBodyFeature
    {
        Task<object?> GetBodyAsync(Type bodyType);

        public bool IsBadRequestException(Exception ex, out bool preventRethrow)
        {
            preventRethrow = false;
            return false;
        }
    }
}
