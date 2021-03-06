﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Protocol;
using InvocationMessage = Microsoft.Azure.SignalR.Serverless.Protocols.InvocationMessage;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    internal class SignalRInvocationMethodExecutor: SignalRMethodExecutor
    {
        public SignalRInvocationMethodExecutor(IRequestResolver resolver, ExecutionContext executionContext): base(resolver, executionContext)
        {
        }

        public override async Task<HttpResponseMessage> ExecuteAsync(HttpRequestMessage request)
        {
            if (!Resolver.TryGetInvocationContext(request, out var context))
            {
                //TODO: More detailed exception
                throw new SignalRTriggerException();
            }
            var (message, protocol) = await Resolver.GetMessageAsync<InvocationMessage>(request);
            AssertConsistency(context, message);
            context.Arguments = message.Arguments;

            // Only when it's an invoke, we need the result from function execution.
            TaskCompletionSource<object> tcs = null;
            if (!string.IsNullOrEmpty(message.InvocationId))
            {
                tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            HttpResponseMessage response;
            CompletionMessage completionMessage = null;

            var functionResult = await ExecuteWithAuthAsync(request, ExecutionContext, context, tcs);
            if (tcs != null)
            {
                if (!functionResult.Succeeded)
                {
                    // TODO: Consider more error details
                    completionMessage = CompletionMessage.WithError(message.InvocationId, "Execution failed");
                    response = new HttpResponseMessage(HttpStatusCode.OK);
                }
                else
                {
                    var result = await tcs.Task;
                    completionMessage = CompletionMessage.WithResult(message.InvocationId, result);
                    response = new HttpResponseMessage(HttpStatusCode.OK);
                }
            }
            else
            {
                response = new HttpResponseMessage(HttpStatusCode.OK);
            }

            if (completionMessage != null)
            {
                response.Content = new ByteArrayContent(protocol.GetMessageBytes(completionMessage).ToArray());
            }
            return response;
        }

        private void AssertConsistency(InvocationContext context, InvocationMessage message)
        {
            if (!string.Equals(context.Event, message.Target, StringComparison.OrdinalIgnoreCase))
            {
                // TODO: More detailed exception
                throw new SignalRTriggerException();
            }
        }
    }
}
