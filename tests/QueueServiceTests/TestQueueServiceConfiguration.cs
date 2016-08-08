// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.Common.Queue
{
    using Newtonsoft.Json;
    using System;
    using Common;
    using QueueService;

    /// <summary>
    /// QueueService configuration class.
    /// </summary>
    [JsonObject(MemberSerialization=MemberSerialization.OptOut)]
    public class TestQueueServiceConfiguration : IQueueServiceConfiguration
    {
        #region PriorityQueueService Configuration

        public int MyTestConfigurationValue { get; private set; } = 31662;

        public int MyTestValue_V2 { get; private set; } = 100;

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
        public double CapacityErrorPercent { get; private set; } = 0.90;

        /// <summary>
        /// Maximum number of items that can be dequeued at once.
        /// </summary>
        [JsonProperty]
        public int MaximumDequeueCount { get; private set; } = 5;

        /// <summary>
        /// Number of priorities for this instance.
        /// </summary>
        [JsonProperty]
        public int NumberOfQueues { get; private set; } = 10;

        /// <summary>
        /// Lease item percent of queue capacity before warning.
        /// </summary>
        [JsonProperty]
        public double LeaseItemPercentWarning { get; private set; } = 0.20;

        /// <summary>
        /// Lease item percent of queue capacity before error.
        /// </summary>
        [JsonProperty]
        public double LeaseItemPercentError { get; private set; } = 0.40;

        /// <summary>
        /// Lease item percent of queue capacity before error.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(JsonTimeSpanConverter))]
        public TimeSpan LeaseDuration { get; private set; } = TimeSpan.FromSeconds(30);

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
        /// Time interval before lease checks start.
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
        /// Time interval before health checks start.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(JsonTimeSpanConverter))]
        public TimeSpan HealthCheckStartDelay { get; private set; } = TimeSpan.FromMinutes(2);
        
        /// <summary>
        /// The interval the health check callback will be executed.
        /// </summary>
        [JsonProperty]
        [JsonConverter(typeof(JsonTimeSpanConverter))]
        public TimeSpan HealthCheckInterval { get; private set; } = TimeSpan.FromSeconds(60);

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
