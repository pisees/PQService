// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("QueueServiceBaseTests")]

namespace QuickService.QueueService
{
    using Common;
    using System;

    /// <summary>
    /// QueueService configuration interface.
    /// </summary>
    public interface IQueueServiceConfiguration 
    {
        /// <summary>
        /// Maximum number of items that may be held within a single partition.
        /// </summary>
        int MaxQueueCapacityPerPartition { get; }

        /// <summary>
        /// Maximum number of items that may be leased within a single partition.
        /// </summary>
        int MaxLeaseCapacityPerPartition { get; }

        /// <summary>
        /// Maximum number of items that may be in error or expired within a single partition.
        /// </summary>
        int MaxExpiredCapacityPerPartition { get; }

        /// <summary>
        /// Percent of capacity where a health warning will be raised.
        /// </summary>
        double CapacityWarningPercent { get; }

        /// <summary>
        /// Percent of capacity where a health error will be raised.
        /// </summary>
        double CapacityErrorPercent { get; }

        /// <summary>
        /// Maximum number of items that can be dequeued at once.
        /// </summary>
        int MaximumDequeueCount { get; }

        /// <summary>
        /// Number of priorities for this instance.
        /// </summary>
        int NumberOfQueues { get; }

        /// <summary>
        /// Lease item percent of queue capacity before warning.
        /// </summary>
        double LeaseItemPercentWarning { get; }

        /// <summary>
        /// Lease item percent of queue capacity before error.
        /// </summary>
        double LeaseItemPercentError { get; }

        /// <summary>
        /// Lease item percent of queue capacity before error.
        /// </summary>
        TimeSpan LeaseDuration { get; }

        /// <summary>
        /// Operation timeout for fabric operations in seconds.
        /// </summary>
        TimeSpan FabricOperationTimeout { get; }

        /// <summary>
        /// Duration for a single item to expire.
        /// </summary>
        TimeSpan ItemExpiration { get; }

        /// <summary>
        /// Time interval before the health timer first event is fired.
        /// </summary>
        TimeSpan HealthCheckStartDelay { get; }

        /// <summary>
        /// Time interval between health checks.
        /// </summary>
        TimeSpan HealthCheckInterval { get; }

        /// <summary>
        /// Time interval before the lease timer first event is fired.
        /// </summary>
        TimeSpan LeaseCheckStartDelay { get; }

        /// <summary>
        /// Time interval between checks for lease expiration.
        /// </summary>
        TimeSpan LeaseCheckInterval { get; }

        /// <summary>
        /// Title of the health report for leased items.
        /// </summary>
        string LeasedItemsHealthReportTitle { get; }
        
        /// <summary>
        /// Title of the health report for expired items.
        /// </summary>
        string ExpiredItemsHealthReportTitle { get; }
        
        /// <summary>
        /// Title of the health report for queued items.
        /// </summary>
        string QueuedItemsHealthReportTitle { get; }
        
        /// <summary>
        /// Title of the health report for all items.
        /// </summary>
        string ItemsHealthReportTitle { get; }
        
        /// <summary>
        /// Title of the load report for RPS.
        /// </summary>
        string RequestsPerSecondLoadReportTitle { get; }

        /// <summary>
        /// Title of the load report for queue length.
        /// </summary>
        string QueueLengthLoadReportTitle { get; }

    }
}
