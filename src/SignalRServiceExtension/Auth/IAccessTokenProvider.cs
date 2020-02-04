﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    public interface IAccessTokenProvider
    {
        AccessTokenResult ValidateToken(HttpRequest request);
    }
}