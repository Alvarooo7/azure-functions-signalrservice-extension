﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Protocols;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.WebJobs.Extensions.SignalRService
{
    // todo: extend to implement ITriggerBinding
    // Helper class for implementing IBinding with the attribute resolver pattern. 
    internal abstract class BindingBase<TAttribute> : IBinding
        where TAttribute : Attribute
    {
        protected readonly AttributeCloner<TAttribute> Cloner;
        private readonly ParameterDescriptor param;

        public BindingBase(BindingProviderContext context, IConfiguration configuration, INameResolver nameResolver)
        {
            var attributeSource = TypeUtility.GetResolvedAttribute<TAttribute>(context.Parameter);
            Cloner = new AttributeCloner<TAttribute>(attributeSource, context.BindingDataContract, configuration, nameResolver);

            param = new ParameterDescriptor
            {
                Name = context.Parameter.Name,
                DisplayHints = new ParameterDisplayHints
                {
                    Description = "value"
                }
            };
        }

        public bool FromAttribute
        {
            get
            {
                return true;
            }
        }

        protected abstract Task<IValueProvider> BuildAsync(TAttribute attrResolved, IReadOnlyDictionary<string, object> bindingContext);

        public async Task<IValueProvider> BindAsync(BindingContext context)
        {
            var attrResolved = Cloner.ResolveFromBindingData(context);
            return await BuildAsync(attrResolved, context.BindingData);
        }

        public Task<IValueProvider> BindAsync(object value, ValueBindingContext context)
        {
                //todo [wanl]: figure out what will trigger it
                throw new NotImplementedException();
        }

        public ParameterDescriptor ToParameterDescriptor()
        {
            return param;
        }
    }
}

