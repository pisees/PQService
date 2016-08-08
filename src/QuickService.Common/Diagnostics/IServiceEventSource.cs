// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.Common.Diagnostics
{
    using System;
    using System.Fabric;

    /// <summary>
    /// Defines the minimum events to support logging and diagnostics.
    /// </summary>
    public interface IServiceEventSource
    {
        /// <summary>
        /// A service instance was started for the service type.
        /// </summary>
        /// <param name="serviceTypeName">Name of the service type.</param>
        /// <param name="partition">Guid representing the partition identifier.</param>
        /// <param name="replicaOrInstance">Long integer value representing the replica or instance identifier.</param>
        void ServiceInstanceConstructed(string serviceTypeName, Guid partition, long replicaOrInstance);

        /// <summary>
        /// Start request event.
        /// </summary>
        /// <param name="serviceTypeName">Name of the service type.</param>
        /// <param name="partition">Guid representing the partition identifier.</param>
        /// <param name="replicaOrInstance">Long integer value representing the replica or instance identifier.</param>
        /// <param name="requestTypeName">Name of the request type.</param>
        void ServiceRequestStart(string serviceTypeName, Guid partition, long replicaOrInstance, string requestTypeName);

        /// <summary>
        /// Stop request event.
        /// </summary>
        /// <param name="serviceTypeName">Name of the service type.</param>
        /// <param name="partition">Guid representing the partition identifier.</param>
        /// <param name="replicaOrInstance">Long integer value representing the replica or instance identifier.</param>
        /// <param name="requestTypeName">Name of the request type.</param>
        /// <param name="duration">Duration of the call in milliseconds.</param>
        void ServiceRequestStop(string serviceTypeName, Guid partition, long replicaOrInstance, string requestTypeName, int duration);

        /// <summary>
        /// Service failure request event.
        /// </summary>
        /// <param name="serviceTypeName">Name of the service type.</param>
        /// <param name="partition">Guid representing the partition identifier.</param>
        /// <param name="replicaOrInstance">Long integer value representing the replica or instance identifier.</param>
        /// <param name="requestTypeName">Name of the request type.</param>
        /// <param name="exception">Reason for the failure.</param>
        void ServiceRequestFailed(string serviceTypeName, Guid partition, long replicaOrInstance, string requestTypeName, string exception);

        /// <summary>
        /// RunAsync invoked event.
        /// </summary>
        /// <param name="serviceTypeName">Name of the service type.</param>
        /// <param name="partition">Guid representing the partition identifier.</param>
        /// <param name="replicaOrInstance">Long integer value representing the replica or instance identifier.</param>
        void RunAsyncInvoked(string serviceTypeName, Guid partition, long replicaOrInstance);

        /// <summary>
        /// Create communication listener event.
        /// </summary>
        /// <param name="serviceTypeName">Name of the service type.</param>
        /// <param name="partition">Guid representing the partition identifier.</param>
        /// <param name="replicaOrInstance">Long integer value representing the replica or instance identifier.</param>
        /// <param name="listenAddress">String containing the uri where the communication listener is listening.</param>
        void CreateCommunicationListener(string serviceTypeName, Guid partition, long replicaOrInstance, string listenAddress);

        /// <summary>
        /// Service partition configuration changed event.
        /// </summary>
        /// <param name="serviceTypeName">Name of the service type.</param>
        /// <param name="partition">Guid representing the partition identifier.</param>
        /// <param name="replicaOrInstance">Long integer value representing the replica or instance identifier.</param>
        void ServicePartitionConfigurationChanged(string serviceTypeName, Guid partition, long replicaOrInstance);

        /// <summary>
        /// Service partition may have experienced data loss.
        /// </summary>
        /// <param name="serviceTypeName">Name of the service type.</param>
        /// <param name="partition">Guid representing the partition identifier.</param>
        /// <param name="replicaOrInstance">Long integer value representing the replica or instance identifier.</param>
        void PotentialDataLoss(string serviceTypeName, Guid partition, long replicaOrInstance);

        /// <summary>
        /// OnOpenAsync invoked event.
        /// </summary>
        /// <param name="serviceTypeName">Name of the service type.</param>
        /// <param name="partition">Guid representing the partition identifier.</param>
        /// <param name="replicaOrInstance">Long integer value representing the replica or instance identifier.</param>
        void OpenAsyncInvoked(string serviceTypeName, Guid partition, long replicaOrInstance);

        /// <summary>
        /// OnChangeRoleAsync invoked event.
        /// </summary>
        /// <param name="serviceTypeName">Name of the service type.</param>
        /// <param name="partition">Guid representing the partition identifier.</param>
        /// <param name="replicaOrInstance">Long integer value representing the replica or instance identifier.</param>
        /// <param name="role">Name of the new role.</param>
        void ChangeRoleAsyncInvoked(string serviceTypeName, Guid partition, long replicaOrInstance, string role);

        /// <summary>
        /// OnCloseAsync invoked event.
        /// </summary>
        /// <param name="serviceTypeName">Name of the service type.</param>
        /// <param name="partition">Guid representing the partition identifier.</param>
        /// <param name="replicaOrInstance">Long integer value representing the replica or instance identifier.</param>
        void CloseAsyncInvoked(string serviceTypeName, Guid partition, long replicaOrInstance);

        /// <summary>
        /// OnAbort invoked event.
        /// </summary>
        /// <param name="serviceTypeName">Name of the service type.</param>
        /// <param name="partition">Guid representing the partition identifier.</param>
        /// <param name="replicaOrInstance">Long integer value representing the replica or instance identifier.</param>
        void AbortInvoked(string serviceTypeName, Guid partition, long replicaOrInstance);
    }
}
