//  <copyright file="ClusterHelper.cs" company="Microsoft Corporation">
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// </copyright>

namespace QuickService.Common
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Script.Serialization;
    using ErrorHandling;

    /// <summary>
    /// Service configuration discovery helper class.
    /// </summary>
    public static class ClusterHelpers
    {
        #region Constants

        /// <summary>
        /// Name of the local host.
        /// </summary>
        public const string Localhost = "localhost";

        /// <summary>
        /// Name of the partition description key.
        /// </summary>
        public const string PartitionDescription = "PartitionDescription";

        #endregion

        /// <summary>
        /// Shared FabricClient instance. This code must run on a node with service fabric installed.
        /// </summary>
        private static readonly FabricClient _client = new FabricClient();

        /// <summary>
        /// JavaScript serializer instance.
        /// </summary>
        private static readonly JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();

        private static RetryPolicy s_rp = new RetryPolicy(5, 
                                    new ExponentialBackoffWaitingPolicy(TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(20)), 
                                    new RestApiErrorDetectionStrategy());

        #region Public Methods

        /// <summary>
        /// Retrieves the number of partitions for a named service instance.
        /// </summary>
        /// <param name="serviceUri">Uri containing the service name.</param>
        /// <returns>Count of the number of partitions.</returns>
        public static async Task<int> GetPartitionCountAsync(Uri serviceUri)
        {
            int count = 0;

            // Get the ServiceDescription.
            var sd = await _client.ServiceManager.GetServiceDescriptionAsync(serviceUri).ConfigureAwait(false);

            if (sd.PartitionSchemeDescription is SingletonPartitionSchemeDescription)
            {
                count = 1;
            }
            else if (sd.PartitionSchemeDescription is NamedPartitionSchemeDescription)
            {
                count = ((NamedPartitionSchemeDescription)sd.PartitionSchemeDescription).PartitionNames.Count;
            }
            else if (sd.PartitionSchemeDescription is UniformInt64RangePartitionSchemeDescription)
            {
                count = ((UniformInt64RangePartitionSchemeDescription)sd.PartitionSchemeDescription).PartitionCount;
            }

            // Wait for the task to complete.
            return count;
        }

        #endregion
    }
}
