﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Azure.SignalR.Serverless.Protocols;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRRequestResolver : IRequestResolver
    {
        private readonly bool _validateSignature;

        // Now it's only used in test, but when the trigger started to support AAD,
        // It can be configurable in public.
        internal SignalRRequestResolver(bool validateSignature = true)
        {
            _validateSignature = validateSignature;
        }

        public bool ValidateContentType(HttpRequestMessage request)
        {
            var contentType = request.Content.Headers.ContentType.MediaType;
            if (string.IsNullOrEmpty(contentType))
            {
                return false;
            }
            return contentType == Constants.JsonContentType || contentType == Constants.MessagePackContentType;
        }

        // The algorithm is defined in spec: Hex_encoded(HMAC_SHA256(access-key, connection-id))
        public bool ValidateSignature(HttpRequestMessage request, string accessToken)
        {
            if (!_validateSignature)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(accessToken) &&
                request.Headers.TryGetValues(Constants.AsrsSignature, out var values))
            {
                var signatures = SignalRTriggerUtils.GetSignatureList(values.FirstOrDefault());
                if (signatures == null)
                {
                    return false;
                }
                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(accessToken)))
                {
                    var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(request.Headers.GetValues(Constants.AsrsConnectionIdHeader).First()));
                    var hash = "sha256=" + BitConverter.ToString(hashBytes).Replace("-", "");
                    return signatures.Contains(hash, StringComparer.OrdinalIgnoreCase);
                }
            }

            return false;
        }

        public bool TryGetInvocationContext(HttpRequestMessage request, out InvocationContext context)
        {
            context = new InvocationContext();
            // Required properties
            context.ConnectionId = request.Headers.GetValues(Constants.AsrsConnectionIdHeader).FirstOrDefault();
            if (string.IsNullOrEmpty(context.ConnectionId))
            {
                return false;
            }
            context.Hub = request.Headers.GetValues(Constants.AsrsHubNameHeader).FirstOrDefault();
            context.Category = request.Headers.GetValues(Constants.AsrsCategory).FirstOrDefault();
            context.Event = request.Headers.GetValues(Constants.AsrsEvent).FirstOrDefault();
            // Optional properties
            if (request.Headers.TryGetValues(Constants.AsrsUserId, out var values))
            {
                context.UserId = values.FirstOrDefault();
            }
            if (request.Headers.TryGetValues(Constants.AsrsClientQueryString, out values))
            {
                context.Query = SignalRTriggerUtils.GetQueryDictionary(values.FirstOrDefault());
            }
            if (request.Headers.TryGetValues(Constants.AsrsUserClaims, out values))
            {
                context.Claims = SignalRTriggerUtils.GetClaimDictionary(values.FirstOrDefault());
            }
            context.Headers = SignalRTriggerUtils.GetHeaderDictionary(request);

            return true;
        }

        public async Task<(T, IHubProtocol)> GetMessageAsync<T>(HttpRequestMessage request) where T : ServerlessMessage, new()
        {
            var payload = new ReadOnlySequence<byte>(await request.Content.ReadAsByteArrayAsync());
            var messageParser = MessageParser.GetParser(request.Content.Headers.ContentType.MediaType);
            if (!messageParser.TryParseMessage(ref payload, out var message))
            {
                throw new SignalRTriggerException("Parsing message failed");
            }

            return ((T)message, messageParser.Protocol);
        }
    }
}
