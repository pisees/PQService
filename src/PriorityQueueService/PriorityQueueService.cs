// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.ServiceFabric.Services.Communication.Runtime;
using QuickService.Common;
using QuickService.Common.Rest;
using System.Collections.Generic;
using System.Fabric;
using QuickService.QueueService;
using QuickService.QueueClient;

namespace QuickService.PriorityQueueService
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance.
    /// </summary>
    public sealed class PriorityQueueService : GenericInt64PartitionService<PriorityQueueServiceConfiguration>
    {
        /// <summary>
        /// Name of the OWin application root.
        /// </summary>
        public const string ApplicationRoot = "pqs";
        
        /// <summary>
        /// Constant containing the name of the service type
        /// </summary>
        public const string ServiceTypeName = "PriorityQueueServiceType";

        /// <summary>
        /// Number of priorities in the queue.
        /// </summary>
        public const int PriorityCount = 5;

        /// <summary>
        /// QueuePartitionOperations instance.
        /// </summary>
        private QueuePartitionOperations<Item, PriorityQueueServiceConfiguration> _partitionOperations = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityQueueService"/> class. 
        /// </summary>
        public PriorityQueueService(StatefulServiceContext context) 
            : base(context, ServiceEventSource.Current)
        {
            ServiceEventSource.Current.ServiceInstanceConstructed(Context.ServiceName.AbsoluteUri, Context.PartitionId, Context.ReplicaOrInstanceId);

            // Create the QueuePartitionOperations instance and set the based class IGenericService property.
            _partitionOperations = new QueuePartitionOperations<Item, PriorityQueueServiceConfiguration>(this, ServiceEventSource.Current, PriorityCount, TokenSource.Token);
        }        

        /// <summary>
        /// Optional override to create listeners (like TCP, HTTP) for this service replica.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[]
            {
                new ServiceReplicaListener(context => new StatefulOwinCommunicationListener<QueuePartitionOperations<Item, PriorityQueueServiceConfiguration>>(ApplicationRoot, _partitionOperations, context, ServiceEventSource.Current), "OwinListener", false)
            };
        }
    }
}
