// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    // todo [wanl]: remove hardcode key = hubName, HubName in attribute already marked as [AutoResolve], its behavior should be [AutoResolve].
    // Then all resolve jobs are put in resolvers, we can also remove the SignalROption after we apply resolve jobs inside bindings.
    
    /// <summary>
    /// Extension methods for SignalR Service integration
    /// </summary>
    public static class SignalRWebJobsBuilderExtensions
    {
        /// <summary>
        /// Adds the SignalR extension to the provided <see cref="IWebJobsBuilder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IWebJobsBuilder"/> to configure.</param>
        public static IWebJobsBuilder AddSignalR(this IWebJobsBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddExtension<SignalRConfigProvider>()
                .ConfigureOptions<SignalROptions>(ApplyConfiguration);

            return builder;
        }

        private static void ApplyConfiguration(IConfiguration config, SignalROptions options)
        {
            if (config == null)
            {
                return;
            }

            config.Bind(options);

            var hubName = config.GetValue<string>("hubName");
            if (!string.IsNullOrEmpty(hubName))
            {
                options.HubName = hubName;
            }
        }
    }
}