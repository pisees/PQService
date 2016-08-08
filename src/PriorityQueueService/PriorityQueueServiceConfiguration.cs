// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.PriorityQueueService
{
    using Newtonsoft.Json;
    using Common;
    using System;    
    using QueueService;

    /// <summary>
    /// PriorityQueueService configuration class.
    /// </summary>
    /// <remarks>The ConfigurationProvider class uses Json.NET to serialize the class internally. All of the properties should be exposed as read only properties,
    /// requiring public getter, but private setters. This class is used by the QueuePartitionOperation and GenericInt64StatefulService classes via the interfaces
    /// below. THe class must be marked using the JsonObjectAttribute and each property must be marked with the JsonPropertyAttribute. Any non standard types
    /// must use a JsonConverter like the TimeSpan based properties below.</remarks>
    [JsonObject(MemberSerialization=MemberSerialization.OptOut)]
    public class PriorityQueueServiceConfiguration : IQueueServiceConfiguration
    {
        #region PriorityQueueService Configuration

        // TODO: Add any priority queue service specific configuration values here.

        #endregion

        #region IQueueServiceConfiguration

        /// <summary>
        /// Maximum number of items that may be held within a single partition.
        /// </summary>
        [JsonProperty]
        public int MaxQueueCapacityPerPartition { get; private set; } = 100000;

        /// <summary>
        /// Maximum number of items that may be leased within a single partition.
        /// </summary>
        [JsonProperty]
        public int MaxLeaseCapacityPerPartition { get; private set; } = 10000;

        /// <summary>
        /// Maximum number of items that may be in error or expired within a single partition.
        /// </summary>
        [JsonProperty]
        public int MaxExpiredCapacityPerPartition { get; private set; } = 100;

        /// <summary>
        /// Percent of capacity where a health warning will be raised.
        /// </summary>
        [JsonProperty]
        public double CapacityWarningPercent { get; private set; } = 0.75;

        /// <summary>
        /// Percent of capacity where a health error will be raised.
        /// </summary>
        [JsonProperty]
        public double CapacityErrorPercent { get; private set; } = 0.95;

        /// <summary>
        /// Maximum number of items that can be dequeued at once.
        /// </summary>
        [JsonProperty]
        public int MaximumDequeueCount { get; private set; } = 5;

        /// <summary>
        /// Number of priorities for this instance.
        /// </summary>
        [JsonProperty]
        public int NumberOfQueues { get; private set; } = 5;

        /// <summary>
        /// Lease item percent of queue capacity before warning.
        /// </summary>
        [JsonProperty]
        public double LeaseItemPercentWarning { get; private set; } = 0.80;

        /// <summary>
        /// Lease item percent of queue capacity before error.
        /// </summary>
        [JsonProperty]
        public double LeaseItemPercentError { get; private set; } = 0.99;

        /// <summary>
        /// Lease item percent of queue capacity before error.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(JsonTimeSpanConverter))]
        public TimeSpan LeaseDuration { get; private set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Operation timeout for fabric operations in seconds.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(JsonTimeSpanConverter))]
        public TimeSpan FabricOperationTimeout { get; private set; } = TimeSpan.FromSeconds(4);

        /// <summary>
        /// Duration for a single item to expire.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(JsonTimeSpanConverter))]
        public TimeSpan ItemExpiration { get; private set; } = TimeSpan.MaxValue;

        /// <summary>
        /// Time interval between health checks.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(JsonTimeSpanConverter))]
        public TimeSpan HealthCheckStartDelay { get; private set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Time interval delay before health checks begin.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(JsonTimeSpanConverter))]
        public TimeSpan HealthCheckInterval { get; private set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Time interval between health checks.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(JsonTimeSpanConverter))]
        public TimeSpan LeaseCheckStartDelay { get; private set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// Time interval between checks for lease expiration.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(JsonTimeSpanConverter))]
        public TimeSpan LeaseCheckInterval { get; private set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Title of the health report for leased items.
        /// </summary>
        [JsonProperty]
        public string LeasedItemsHealthReportTitle { get; private set; } = "LeasedItems";

        /// <summary>
        /// Title of the health report for expired items.
        /// </summary>
        [JsonProperty]
        public string ExpiredItemsHealthReportTitle { get; private set; } = "ExpiredItems";

        /// <summary>
        /// Title of the health report for queued items.
        /// </summary>
        [JsonProperty]
        public string QueuedItemsHealthReportTitle { get; private set; } = "WaitingItems";

        /// <summary>
        /// Title of the health report for all items.
        /// </summary>
        [JsonProperty]
        public string ItemsHealthReportTitle { get; private set; } = "TotalItems";

        /// <summary>
        /// Title of the load report for RPS.
        /// </summary>
        [JsonProperty]
        public string RequestsPerSecondLoadReportTitle { get; private set; } = "RPS";

        /// <summary>
        /// Title of the load report for queue length.
        /// </summary>
        [JsonProperty]
        public string QueueLengthLoadReportTitle { get; private set; } = "QueueLength";

        #endregion
    }
}
