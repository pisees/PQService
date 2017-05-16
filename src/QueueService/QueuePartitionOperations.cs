// ------------------------------------------------------------
//  <copyright file="QueuePartitionOperation.cs" Company="Microsoft Corporation">
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// </copyright>
// ------------------------------------------------------------
namespace QuickService.QueueService
{
    using Common;
    using Common.Configuration;
    using Common.Diagnostics;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Services.Remoting;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Fabric;
    using QueueClient;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Stateful queue service base class.
    /// </summary>
    /// <typeparam name="TItem">Type of the item contained by the queue.</typeparam>
    /// <typeparam name="TConfiguration">Type of the configuration structure.</typeparam>
    public sealed class QueuePartitionOperations<TItem, TConfiguration> : IQueueService<TItem>, IService, IDisposable
        where TItem : IEquatable<TItem>
        where TConfiguration : class, IQueueServiceConfiguration, new()
    {
        #region Constants

        /// <summary>
        /// Name of the items dictionary.
        /// </summary>
        private const string c_ItemDictionaryName = "Items";

        /// <summary>
        /// Name of the leased items dictionary.
        /// </summary>
        private const string c_LeasedItemDictionaryName = "LeasedItems";

        /// <summary>
        /// Name of the expired items dictionary.
        /// </summary>
        private const string c_ExpiredItemDictionaryName = "ExpiredItems";

        /// <summary>
        /// Template for the name of each item within the array of reliable queues.
        /// </summary>
        private const string c_QueueNameTemplate = "_queue_{0}";

        /// <summary>
        /// Maximum number of queues per partition.
        /// </summary>
        private const int c_MaxQueues = 100;

        #endregion

        #region Private Fields

        /// <summary>
        /// Static empty list of QueueItem&lt;TItem&gt;.
        /// </summary>
        private static readonly IEnumerable<QueueItem<TItem>> EmptyQueueItemList = Enumerable.Empty<QueueItem<TItem>>();

        /// <summary>
        /// Static empty list of Boolean values.
        /// </summary>
        private static readonly IEnumerable<bool> EmptyBooleanList = Enumerable.Empty<bool>();

        /// <summary>
        /// Static fabric client instance used for interacting with service fabric infrastructure.
        /// </summary>
        private FabricClient ServiceFabricClient => GenericInt64PartitionService<TConfiguration>.ServiceFabricClient;

        /// <summary>
        /// IReliableStateManager instance to interact with the Service Fabric state store from the service.
        /// </summary>
        private readonly GenericInt64PartitionService<TConfiguration> _service = null;

        /// <summary>
        /// Gets a IReliableStateManager instance.
        /// </summary>
        private IReliableStateManager StateManager => _service.StateManager;

        /// <summary>
        /// Gets the configuration provider of type QueueServiceConfiguration from the service.
        /// </summary>
        private IConfigurationProvider<TConfiguration> ConfigurationProvider => _service.ConfigurationProvider;

        /// <summary>
        /// Gets the StatefulServiceContext instance from the service.
        /// </summary>
        private StatefulServiceContext Context => _service.Context;

        /// <summary>
        /// Get a IQueueServiceConfiguration instance from the service.
        /// </summary>
        private IQueueServiceConfiguration QueueConfig => _service.ConfigurationProvider.Config;

        /// <summary>
        /// Time the next lease expires.
        /// </summary>
        private DateTimeOffset _nextLeaseExpiration = DateTimeOffset.MaxValue;

        /// <summary>
        /// IQueueEventSource instance to log events.
        /// </summary>
        private readonly IQueueEventSource _eventSource = null;

        /// <summary>
        /// Identifier used for the Health source.
        /// </summary>
        private const string _healthSourceId = "QueueOperation";

        /// <summary>
        /// Tracks the number of request per second to this service.
        /// </summary>
        private CountPerSecond _requestsPerSecond;

        /// <summary>
        /// Tracks the average call latency for the dequeue operation.
        /// </summary>
        private AverageLatency _avgDequeueLatency;

        /// <summary>
        /// Tracks the average call latency for the enqueue operation.
        /// </summary>
        private AverageLatency _avgEnqueueLatency;

        /// <summary>
        /// Tracks the average call latency for the release operation.
        /// </summary>
        private AverageLatency _avgExtendLeaseLatency;

        /// <summary>
        /// Number of queues per partition in this queue service.
        /// </summary>
        private string[] _queueNames = null;

        /// <summary>
        /// Lease timer.
        /// </summary>
        private Timer _leaseTimer = null;

        /// <summary>
        /// Health report timer.
        /// </summary>
        private Timer _healthTimer = null;

        /// <summary>
        /// CancellationTokenSource for this set of operations.
        /// </summary>
        private CancellationToken _cancellationToken;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// QueueService constructor.
        /// </summary>
        /// <param name="service">Service instance.</param>
        /// <param name="eventSource">EventSource methods used by this base class.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        public QueuePartitionOperations(GenericInt64PartitionService<TConfiguration> service, IQueueEventSource eventSource, CancellationToken cancellationToken)
            : this(service, eventSource, 1, cancellationToken)
        {
        }

        /// <summary>
        /// QueueService constructor.
        /// </summary>
        /// <param name="service">Service instance.</param>
        /// <param name="eventSource">EventSource methods used by this base class.</param>
        /// <param name="queues">The number of queues to create.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        public QueuePartitionOperations(GenericInt64PartitionService<TConfiguration> service, IQueueEventSource eventSource, int queues, CancellationToken cancellationToken)
        {
            // Check passed parameters.
            Guard.ArgumentNotNull(service, nameof(service));
            Guard.ArgumentNotNull(eventSource, nameof(eventSource));
            Guard.ArgumentInRange(queues, 1, c_MaxQueues, nameof(queues));

            _service = service;
            _eventSource = eventSource;
            _cancellationToken = cancellationToken;

            // Create the array of queue names and then populate with the names.
            _queueNames = new string[queues];
            for(int i=0; i < _queueNames.Length; i++)
            {
                _queueNames[i] = string.Format(c_QueueNameTemplate, i);
            }

            // Subscribe to the open and change role events.
            service.OnRoleChangeEvent += Service_OnRoleChangedEvent;

            // SEPARATE: out into a separate class and call from the controller or remoting wrapper
            _requestsPerSecond = new CountPerSecond();

        }

        /// <summary>
        /// QueuePartitionOperations destructor.
        /// </summary>
        ~QueuePartitionOperations()
        {
            _service.OnRoleChangeEvent -= Service_OnRoleChangedEvent;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Called when the instance role is changed.
        /// </summary>
        /// <param name="sender">Reference to the stateful service instance.</param>
        /// <param name="e">RoleChangedEventArgs instance.</param>
        private void Service_OnRoleChangedEvent(object sender, RoleChangedEventArgs e)
        {
            switch(e.NewRole)
            {
                case ReplicaRole.ActiveSecondary:
                    if (null != _leaseTimer)
                    {
                        _leaseTimer.Dispose();
                        _leaseTimer = null;
                    }
                    _healthTimer = CreateOrReturnHealthTimer();
                    break;

                case ReplicaRole.Primary:
                    _leaseTimer = new Timer(async (o) => { await ProcessLeasesAsync(_cancellationToken); }, _cancellationToken, QueueConfig.LeaseCheckStartDelay, QueueConfig.LeaseCheckInterval);
                    _healthTimer = CreateOrReturnHealthTimer();
                    break;

                default:
                    if (null != _leaseTimer)
                    {
                        _leaseTimer.Dispose();
                        _leaseTimer = null;
                    }
                    if (null != _healthTimer)
                    {
                        _healthTimer.Dispose();
                        _healthTimer = null;
                    }
                    break;      
            }
        }

        /// <summary>
        /// Creates a new health timer or returns the existing one.
        /// </summary>
        /// <returns>Timer for generating health events.</returns>
        private Timer CreateOrReturnHealthTimer()
        {
            if (null != _healthTimer)
                return _healthTimer;

            return new Timer(async (o) => { await ReportHealthAsync(_cancellationToken); }, _cancellationToken, QueueConfig.HealthCheckStartDelay, QueueConfig.HealthCheckInterval);
        }

        /// <summary>
        /// Handles processing of items whose lease has expired.
        /// </summary>
        /// <param name="tx">ITransaction to use.</param>
        /// <param name="key">PopReceipt containing the unique key for the item.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns>True if successful, otherwise false.</returns>
        /// <remarks>Treat each leased item as a transaction to minimize the chance of items getting "stuck" because of a later failure.
        /// If the lease was expired, remove the item from the dictionary and add back to the queue if the expiration time hasn't passed.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null or cannot be serialized.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Timeout is negative.</exception>
        /// <exception cref="OperationCanceledException">Operation was canceled.</exception>
        /// <exception cref="TimeoutException">The operation failed to complete within the given timeout specified by FabricOperationTimeout.</exception>
        private async Task<bool> LeaseExpiredAsync(ITransaction tx, PopReceipt key, CancellationToken cancellationToken)
        {
            try
            {
                // Get references to the necessary collections.
                var items = await GetItemsDictionaryAsync().ConfigureAwait(false);
                var expiredItems = await GetExpiredItemsDictionaryAsync().ConfigureAwait(false);
                var leasedItems = await GetLeasedItemsDictionaryAsync().ConfigureAwait(false);

                ConditionalValue<QueueItem<TItem>> ir = await items.TryGetValueAsync(tx, key, LockMode.Update, QueueConfig.FabricOperationTimeout, cancellationToken).ConfigureAwait(false);
                if (ir.HasValue)
                {
                    // We found the item, process the expiration.
                    var item = ir.Value;

                    // Check if it has been dequeued the maximum number of times, if so, add to expired item collection.
                    if (ir.Value.DequeueCount >= QueueConfig.MaximumDequeueCount)
                    {
                        await expiredItems.AddAsync(tx, key, ir.Value, QueueConfig.FabricOperationTimeout, cancellationToken).ConfigureAwait(false);
                        _eventSource.QueueItemExpired(tx.TransactionId, Context.PartitionId, Context.ReplicaId, key.ToString());
                    }
                    else
                    {
                        // Create the updated item. Update the queue to the next queue and reset the leaseUntil. Don't reset the enqueue time.
                        int queue = (item.Queue == 0) ? 0 : item.Queue - 1;
                        var updatedItem = item.UpdateWith(queue, DateTimeOffset.MaxValue);

                        // Update the item and add to the queue.
                        if (await items.TryUpdateAsync(tx, key, updatedItem, item, QueueConfig.FabricOperationTimeout, cancellationToken).ConfigureAwait(false))
                        {
                            var q = await GetQueueAsync(queue).ConfigureAwait(false);
                            await q.EnqueueAsync(tx, key, QueueConfig.FabricOperationTimeout, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                // Remove it from the leased item collection.
                var removedItem = await leasedItems.TryRemoveAsync(tx, key, QueueConfig.FabricOperationTimeout, cancellationToken).ConfigureAwait(false);
                if (removedItem.HasValue)
                {
                    _eventSource.QueueItemLeaseComplete(tx.TransactionId, Context.PartitionId, Context.ReplicaId, key.ToString(), false, (int)(DateTime.UtcNow - ir.Value.EnqueueTime).TotalMilliseconds);
                }

                return removedItem.HasValue;
            }
            catch (InvalidOperationException ex) { _eventSource.QueueItemOperationAborted(Context.PartitionId, Context.ReplicaId, key.ToString(), ex); throw; }
            catch (FabricNotPrimaryException ex) { _eventSource.QueueItemOperationAborted(Context.PartitionId, Context.ReplicaId, key.ToString(), ex); throw; }
        }

        /// <summary>
        /// Updates the lease of a set of items.
        /// </summary>
        /// <param name="tx">ITransaction instance.</param>
        /// <param name="keys">List of item keys to update.</param>
        /// <param name="lease">Duration of the new lease.</param>
        /// <param name="cancellationToken">Cancellation token instance.</param>
        /// <returns>IEnumerable of Boolean values indicating success.</returns>
        private async Task<IEnumerable<bool>> UpdateItemsLeaseAsync(ITransaction tx, IEnumerable<PopReceipt> keys, TimeSpan lease, CancellationToken cancellationToken)
        {
            Guard.ArgumentNotNull(tx, nameof(tx));
            Guard.ArgumentNotNull(keys, nameof(keys));
            Guard.ArgumentInRange(lease.TotalSeconds, 0.0, double.MaxValue, nameof(lease));

            // Track and return the success of each key. Using an array to simplify the error handling below (default Boolean is false).
            int offset = 0;
            bool[] results = new bool[keys.Count()];

            // Get the necessary collections.
            var items = await GetItemsDictionaryAsync().ConfigureAwait(false);
            var leasedItems = await GetLeasedItemsDictionaryAsync().ConfigureAwait(false);

            // Enumerate the list of keys and update each one, recording the success or failure.
            foreach (PopReceipt key in keys)
            {
                // Get the item using the key.
                var itemResults = await items.TryGetValueAsync(tx, key, LockMode.Update, QueueConfig.FabricOperationTimeout, cancellationToken).ConfigureAwait(false);
                if (true == itemResults.HasValue)
                {
                    // Update the lease duration on the item and the lease.
                    QueueItem<TItem> item = itemResults.Value.UpdateWith(lease, DateTimeOffset.UtcNow.Add(lease));

                    if (await items.TryUpdateAsync(tx, key, item, itemResults.Value, QueueConfig.FabricOperationTimeout, cancellationToken).ConfigureAwait(false))
                    {
                        var dt = await leasedItems.AddOrUpdateAsync(tx, key, item.LeasedUntil, (k, v) => { return item.LeasedUntil; }, QueueConfig.FabricOperationTimeout, cancellationToken).ConfigureAwait(false);
                        if (dt == item.LeasedUntil)
                        {
                            _eventSource.QueueItemLeaseExtended(lease.TotalSeconds, key.ToString(), Context.PartitionId, Context.ReplicaId);
                            results[offset] = true;
                        }
                    }
                }
                else
                {
                    _eventSource.QueueItemNotFound(key.ToString(), Context.PartitionId, Context.ReplicaOrInstanceId);
                }

                // Increment the array offset.
                offset++;
            }

            return results;
        }

        /// <summary>
        /// Removes a leased item.
        /// </summary>
        /// <param name="tx">ITransaction instance.</param>
        /// <param name="keys">List of item keys to update.</param>
        /// <param name="cancellationToken">Cancellation token instance.</param>
        /// <returns>IEnumerable of Boolean values indicating success.</returns>
        private async Task<IEnumerable<bool>> RemoveLeasedItemsAsync(ITransaction tx, IEnumerable<PopReceipt> keys, CancellationToken cancellationToken)
        {
            Guard.ArgumentNotNull(tx, nameof(tx));
            Guard.ArgumentNotNull(keys, nameof(keys));

            int offset = 0;
            bool[] results = new bool[keys.Count()];

            // Get the necessary collections.
            var items = await GetItemsDictionaryAsync().ConfigureAwait(false);
            var expiredItems = await GetExpiredItemsDictionaryAsync().ConfigureAwait(false);
            var leasedItems = await GetLeasedItemsDictionaryAsync().ConfigureAwait(false);

            // Enumerate the list of keys and update each one, recording the success or failure.
            foreach (PopReceipt key in keys)
            {
                // Remove both the item and the leasedItem.
                var leasedResult = await leasedItems.TryRemoveAsync(tx, key, QueueConfig.FabricOperationTimeout, cancellationToken).ConfigureAwait(false);
                if (leasedResult.HasValue)
                {
                    var itemResult = await items.TryRemoveAsync(tx, key, QueueConfig.FabricOperationTimeout, cancellationToken).ConfigureAwait(false);
                    if (itemResult.HasValue)
                    {
                        results[offset] = true;
                        _eventSource.QueueItemRemoved(key.ToString(), Context.PartitionId, Context.ReplicaId);
                    }
                    else
                    {
                        _eventSource.QueueItemInvalidLease(key.ToString(), Context.PartitionId, Context.ReplicaId);
                    }
                }
                else
                {
                    _eventSource.QueueItemInvalidLease(key.ToString(), Context.PartitionId, Context.ReplicaId);
                }

                // Advance to the next offset.
                offset++;
            }

            return results;
        }

        /// <summary>
        /// Adds an item to the leased item list.
        /// </summary>
        /// <param name="tx">Transaction instance.</param>
        /// <param name="key">PopReceipt containing the item key.</param>
        /// <param name="item">Item to add to the lease list.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns>QueueItem instance.</returns>
        /// <remarks>Don't commit the transaction within this method, it will be committed in the caller.</remarks>
        private async Task<QueueItem<TItem>> LeaseItemAsync(ITransaction tx, PopReceipt key, QueueItem<TItem> item, CancellationToken cancellationToken)
        {
            // Get the necessary collections.
            var items = await GetItemsDictionaryAsync().ConfigureAwait(false);
            var leasedItems = await GetLeasedItemsDictionaryAsync().ConfigureAwait(false);

            // Create a new instance of the item with the modified properties and update the stored item.
            var updateItem = new QueueItem<TItem>(key, item.Queue, item.Item, item.LeaseDuration, DateTimeOffset.UtcNow.Add(item.LeaseDuration), item.ExpirationTime, item.EnqueueTime, item.DequeueCount + 1);
            if (false == await items.TryUpdateAsync(tx, key, updateItem, item).ConfigureAwait(false))
            {
                tx.Abort();
            }

            // Add or update the item lease.
            await leasedItems.AddOrUpdateAsync(tx, key, updateItem.LeasedUntil, (k, v) => updateItem.LeasedUntil, QueueConfig.FabricOperationTimeout, cancellationToken).ConfigureAwait(false);

            // If this item's lease/expiration will expire sooner than the current value, update the DateTimeOffset.
            // If the transaction is aborted, the worst that will occur from doing this here is that the RunAsync will enumerate the list again sooner than necessary.
            if (_nextLeaseExpiration > updateItem.LeasedUntil)
                _nextLeaseExpiration = updateItem.LeasedUntil;

            _eventSource.Debug($"Item with key {key} was leased until {item.LeasedUntil}");
            return updateItem;
        }

        /// <summary>
        /// Moves an items to the list of expired items.
        /// </summary>
        /// <param name="tx">Transaction instance.</param>
        /// <param name="item">Item to add to the lease list.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns>Task instance.</returns>
        private async Task ExpireItemAsync(ITransaction tx, QueueItem<TItem> item, CancellationToken cancellationToken)
        {
            // Get the necessary collections.
            var items = await GetItemsDictionaryAsync().ConfigureAwait(false);
            var expiredItems = await GetExpiredItemsDictionaryAsync().ConfigureAwait(false);

            // Remove the item from the list of items and add it to the list of expired items.
            var cv = await items.TryRemoveAsync(tx, item.Key, QueueConfig.FabricOperationTimeout, cancellationToken).ConfigureAwait(false);
            if (cv.HasValue)
            {
                await expiredItems.AddAsync(tx, item.Key, item);
            }
        }

        /// <summary>
        /// Checks that all of the keys are within this partition.
        /// </summary>
        /// <param name="keys">Array of keys to check.</param>
        /// <returns>True if all keys are within this partition, otherwise false.</returns>
        private bool CheckKeysInPartition(PopReceipt[] keys)
        {
            Guard.ArgumentNotNull(keys, nameof(keys));

            foreach (PopReceipt pr in keys)
            {
                if (_service.PartitionOffset != pr.Partition)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Reports the health of the service on a periodic basis.
        /// </summary>
        /// <param name="cancellationToken">Contains the cancellation token.</param>
        /// <returns>Task instance.</returns>
        private async Task ReportHealthAsync(CancellationToken cancellationToken)
        {
            _eventSource.QueueMethodStart(Context.PartitionId, Context.ReplicaId, nameof(ReportHealthAsync));

            // Check if the service can be read from.
            if (false == _service.CanRead)
                return;

            try
            {
                long expiredCount = 0;
                long queueCount = 0;
                long itemCount = 0;
                TimeSpan ttl = QueueConfig.HealthCheckInterval;

                // Get the expired item count.
                expiredCount = await CountAsync(QueueType.ExpiredQueue, cancellationToken).ConfigureAwait(false);
                _service.ReportHealthPartitionCapacity(_healthSourceId, QueueConfig.ExpiredItemsHealthReportTitle, expiredCount, QueueConfig.MaxExpiredCapacityPerPartition, QueueConfig.CapacityWarningPercent, QueueConfig.CapacityErrorPercent, ttl);

                // Items in queues.
                queueCount = await CountAsync(QueueType.AllQueues, cancellationToken).ConfigureAwait(false);
                _service.ReportHealthPartitionCapacity(_healthSourceId, QueueConfig.QueuedItemsHealthReportTitle, queueCount, QueueConfig.MaxQueueCapacityPerPartition, QueueConfig.CapacityWarningPercent, QueueConfig.CapacityErrorPercent, ttl);

                // Total number of items in this partition. Don't uncomment this until after collections support non-enumerating counting.
                //itemCount = await CountAsync(QueueType.ItemQueue, cancellationToken).ConfigureAwait(false);
                _service.ReportHealthPartitionCapacity(_healthSourceId, QueueConfig.ItemsHealthReportTitle, itemCount, QueueConfig.MaxQueueCapacityPerPartition, QueueConfig.CapacityWarningPercent, QueueConfig.CapacityErrorPercent, ttl);

                // Report the dequeue, enqueue and extend lease latencies.
                _service.ReportHealthReplicaLatency(_healthSourceId, "Dequeue", _avgDequeueLatency, 1000, 5000, ttl);
                _service.ReportHealthReplicaLatency(_healthSourceId, "Enqueue", _avgEnqueueLatency, 1000, 5000, ttl);
                _service.ReportHealthReplicaLatency(_healthSourceId, "Extend Lease", _avgExtendLeaseLatency, 1000, 5000, ttl);
                _avgDequeueLatency = AverageLatency.Zero;
                _avgEnqueueLatency = AverageLatency.Zero;
                _avgExtendLeaseLatency = AverageLatency.Zero;

                // Get the current count and reset the count.
                long rps = _requestsPerSecond.GetLatest();
                _requestsPerSecond = CountPerSecond.Zero;

                // Report the load on this service as requests per second.
                _service.ReportLoad(new[] { new LoadMetric(QueueConfig.RequestsPerSecondLoadReportTitle, (int)rps), new LoadMetric(QueueConfig.QueueLengthLoadReportTitle, (int)itemCount) });

                // Create a partition health report on request per second.
                _service.ReportHealthRequestPerSecond(_healthSourceId, QueueConfig.RequestsPerSecondLoadReportTitle, rps, ttl: ttl);
            }
            catch (Exception ex)
            {
                // Don't re-throw, an error will be generated when we miss a health report and the logs will indicate what occurred.
                _eventSource.QueueMethodException(Context.PartitionId, Context.ReplicaId, ex);
            }
        }

        /// <summary>
        /// Processes leased items looking for expired leases.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken instance..</param>
        /// <remarks> This must be called at a regular intervals on the primary for each partition.</remarks>
        private async Task ProcessLeasesAsync(CancellationToken cancellationToken)
        {
            int leasedCount = 0;
            int leaseExpiredCount = 0;
            Stopwatch sw = Stopwatch.StartNew();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            // Check if the service can be read from and written to.
            if (false == _service.CanRead || false == _service.CanWrite)
            {
                _eventSource.QueueMethodLogging(nameof(ProcessLeasesAsync), Context.PartitionId, Context.ReplicaId, sw.ElapsedMilliseconds, $"Read status: {_service.CanRead}, Write status: {_service.CanWrite}.");
                return;
            }

            // Get the necessary collections.
            var leasedItems = await GetLeasedItemsDictionaryAsync().ConfigureAwait(false);

            // Run only if the next lease expiration occurs in the past.
            if (_nextLeaseExpiration <= now)
            {
                // Reset the lease expiration because it's currently in the past. This keeps another iteration of the timer from entering
                // this block until the _nextLeaseExpiration is set again at completion.
                DateTimeOffset nextExpiration = _nextLeaseExpiration = DateTimeOffset.MaxValue;

                try
                {
                    using (ITransaction tx = StateManager.CreateTransaction())
                    {
                        // Enumerate the list of items looking for leases that have expired.
                        var enumerable = await leasedItems.CreateEnumerableAsync(tx, EnumerationMode.Unordered).ConfigureAwait(false);
                        var e = enumerable.GetAsyncEnumerator();

                        // Enumerate all leased items looking for expired items. Let this method handle the cancellation token check.
                        while (await e.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                        {
                            leasedCount++;
                            if (e.Current.Value <= now)
                            {
                                if (await LeaseExpiredAsync(tx, e.Current.Key, cancellationToken).ConfigureAwait(false))
                                    leaseExpiredCount++;
                            }

                            // Check if the current item's lease expiration is less than the current next expiration. If so, set it. 
                            // As long as all leased items are being enumerated, this will ensure the lease expiration time is correct.
                            if (nextExpiration > e.Current.Value)
                                nextExpiration = e.Current.Value;
                        }

                        // Commit the transaction and return success.
                        await tx.CommitAsync().ConfigureAwait(false);
                        _eventSource.QueueTransactionCommitted(tx.TransactionId, Context.PartitionId, Context.ReplicaId, tx.CommitSequenceNumber);
                    }

                    // Calculate the health report TTL and then report the leased item capacity numbers.
                    TimeSpan ttl = ((nextExpiration - now) < QueueConfig.HealthCheckInterval) ? QueueConfig.HealthCheckInterval : (nextExpiration - now);
                    _service.ReportHealthPartitionCapacity(_healthSourceId, QueueConfig.LeasedItemsHealthReportTitle, leasedCount, QueueConfig.MaxLeaseCapacityPerPartition, QueueConfig.CapacityWarningPercent, QueueConfig.CapacityErrorPercent, ttl);

                    // Reset the next lease expiration.
                    _nextLeaseExpiration = nextExpiration;
                }
                catch(Exception ex)
                {
                    _eventSource.QueueMethodException(Context.PartitionId, Context.ReplicaId, ex);
                    _nextLeaseExpiration = now;
                }
            }

            // Log the method completion.
            sw.Stop();
            _eventSource.QueueMethodLogging(nameof(ProcessLeasesAsync), Context.PartitionId, Context.ReplicaId, sw.ElapsedMilliseconds, $"Processed {leasedCount} leased items, {leaseExpiredCount} leases expired. Next expiration at {_nextLeaseExpiration}.");
        }

        /// <summary>
        /// Updates the lease duration for set of leased items.
        /// </summary>
        /// <param name="key">PopReceipt key for the item in the leasedItems collection.</param>
        /// <param name="duration">Duration to extend the lease for.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns>Boolean indicating the success or failure.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null or cannot be serialized.</exception>
        /// <exception cref="ArgumentException">FabricOperationTimeout is negative.</exception>
        /// <exception cref="OperationCanceledException">Operation was canceled.</exception>
        /// <exception cref="TimeoutException">The operation failed to complete within the given timeout specified by FabricOperationTimeout.</exception>
        private async Task<bool> UpdateLeaseDurationAsync(PopReceipt key, TimeSpan duration, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Get the necessary collections.
            var items = await GetItemsDictionaryAsync().ConfigureAwait(false);
            var leasedItems = await GetLeasedItemsDictionaryAsync().ConfigureAwait(false);

            using (ITransaction tx = StateManager.CreateTransaction())
            {
                // Get the item using the key.
                ConditionalValue<DateTimeOffset> lr = await leasedItems.TryGetValueAsync(tx, key, LockMode.Update, QueueConfig.FabricOperationTimeout, cancellationToken).ConfigureAwait(false);
                if (true == lr.HasValue)
                {
                    // Get the item from the list of items in order to update the LeaseUntil property.
                    var ir = await items.TryGetValueAsync(tx, key, LockMode.Update, QueueConfig.FabricOperationTimeout, cancellationToken);
                    if (ir.HasValue)
                    {
                        QueueItem<TItem> item = ir.Value;

                        // Make a copy of the item with the new lease value.
                        var clone = new QueueItem<TItem>(key, item.Queue, item.Item, duration, DateTimeOffset.UtcNow.Add(duration), item.ExpirationTime, item.EnqueueTime, item.DequeueCount);
                        if (await leasedItems.TryUpdateAsync(tx, key, clone.LeasedUntil, lr.Value, QueueConfig.FabricOperationTimeout, cancellationToken).ConfigureAwait(false))
                        {
                            if (await items.TryUpdateAsync(tx, key, clone, ir.Value))
                            {
                                await tx.CommitAsync().ConfigureAwait(false);
                                return true;
                            }
                            else
                            {
                                _eventSource.Debug("Item with key {0} could not update the lease in the list of items.", key);
                            }
                        }
                        else
                        {
                            _eventSource.Debug("Item with key {0} could not update the lease duration in the list of leased items.", key);
                        }
                    }
                    else
                    {
                        _eventSource.Debug("Item with key {0} was not found in the list of items.", key);
                    }
                }
                else
                {
                    _eventSource.Debug("Item with key {0} was not found in the list of leased items.", key);
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the item dictionary. If it doesn't already exist it is created.
        /// </summary>
        /// <returns>Reliable dictionary typed of the item dictionary type.</returns>
        /// <remarks>This is a good example of how to initialize reliable collections. Rather than initializing and caching a value, this approach will work on both
        /// primary and secondary replicas (if secondary reads are enabled).</remarks>
        private async Task<IReliableDictionary<PopReceipt, QueueItem<TItem>>> GetItemsDictionaryAsync()
        {
            return await StateManager.GetOrAddAsync<IReliableDictionary<PopReceipt, QueueItem<TItem>>>(c_ItemDictionaryName, QueueConfig.FabricOperationTimeout).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the leased item dictionary. If it doesn't already exist it is created.
        /// </summary>
        /// <returns>Reliable dictionary typed of the leased item dictionary type.</returns>
        /// <remarks>This is a good example of how to initialize reliable collections. Rather than initializing and caching a value, this approach will work on both
        /// primary and secondary replicas (if secondary reads are enabled).</remarks>
        private async Task<IReliableDictionary<PopReceipt, DateTimeOffset>> GetLeasedItemsDictionaryAsync()
        {
            return await this.StateManager.GetOrAddAsync<IReliableDictionary<PopReceipt, DateTimeOffset>>(c_LeasedItemDictionaryName, QueueConfig.FabricOperationTimeout).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the expired item dictionary. If it doesn't already exist it is created.
        /// </summary>
        /// <returns>Reliable dictionary typed of the expired item dictionary type.</returns>
        /// <remarks>This is a good example of how to initialize reliable collections. Rather than initializing and caching a value, this approach will work on both
        /// primary and secondary replicas (if secondary reads are enabled).</remarks>
        private async Task<IReliableDictionary<PopReceipt, QueueItem<TItem>>> GetExpiredItemsDictionaryAsync()
        {
            return await this.StateManager.GetOrAddAsync<IReliableDictionary<PopReceipt, QueueItem<TItem>>>(c_ExpiredItemDictionaryName, QueueConfig.FabricOperationTimeout).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the specified queue. If it doesn't already exist it is created.
        /// </summary>
        /// <param name="index">Index of the queue to retrieve or create.</param>
        /// <returns>Reliable queue instance.</returns>
        /// <remarks>This is a good example of how to initialize reliable collections. Rather than initializing and caching a value, this approach will work on both
        /// primary and secondary replicas (if secondary reads are enabled).</remarks>
        private async Task<IReliableQueue<PopReceipt>> GetQueueAsync(int index)
        {
            Guard.ArgumentInRange(index, 0, _queueNames.Length - 1, nameof(index));

            return await StateManager.GetOrAddAsync<IReliableQueue<PopReceipt>>(_queueNames[index], QueueConfig.FabricOperationTimeout).ConfigureAwait(false);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the number of queues the service is configured for.
        /// </summary>
        /// <returns>Integer value containing the number of queues.</returns>
        /// CHANGE TO Async.
        public int QueueCount()
        {
            _requestsPerSecond += 1;
            return (null == _queueNames) ? 0 : _queueNames.Length;
        }

        /// <summary>
        /// Gets the count from all of the queues in a single partition.
        /// </summary>
        /// <returns>Long value containing the count.</returns>
        /// <remarks>The queue count does not included items that are leased or expired.</remarks>
        public Task<long> CountAsync()
        {
            return CountAsync(QueueType.AllQueues, default(CancellationToken));
        }

        /// <summary>
        /// Gets the count from one of the collections.
        /// </summary>
        /// <param name="queue">Integer indicating the queue to return the count for. 
        /// If QueueClient.AllQueues (-1) is specified, then the count for all queues will be returned.
        /// If QueueClient.LeaseQueue (-2) is specified, then the leased item count will be returned.
        /// If QueueClient.ExpiredQueue (-3) is specified, then the expired collection count will be returned.
        /// If QueueClient.ItemQueue (-4) is specified, then the list of all items queued, leased or expired will be returned.</param>
        /// <param name="ct">CancellationToken. Defaults to CancellationToken.None</param>
        /// <returns>Long value containing the count.</returns>
        /// <remarks>The queue count does not included items that are leased or expired.</remarks>
        /// CHANGE the int to a struct and move the const values to the struct.
        public async Task<long> CountAsync(QueueType queue, CancellationToken ct)
        {
            _requestsPerSecond += 1;
            _eventSource.QueueMethodStart(Context.PartitionId, Context.ReplicaId, nameof(CountAsync));

            // Check that a valid queue is being asked for.
            if ((queue >= _queueNames.Length) || (queue < QueueType.ItemQueue))
            {
                throw new ArgumentOutOfRangeException("queue");
            }

            Stopwatch sw = Stopwatch.StartNew();

            long totalCount = 0;

            try
            {
                // Get the necessary collections.
                var items = await GetItemsDictionaryAsync().ConfigureAwait(false);
                var expiredItems = await GetExpiredItemsDictionaryAsync().ConfigureAwait(false);
                var leasedItems = await GetLeasedItemsDictionaryAsync().ConfigureAwait(false);

                using (ITransaction tx = StateManager.CreateTransaction())
                {
                    switch (queue)
                    {
                        case QueueType.AllQueues:
                            var result = Parallel.For(0, _queueNames.Length, async (i) =>
                            {
                                var q = await GetQueueAsync(i).ConfigureAwait(false);
                                Interlocked.Add(ref totalCount, await q.GetCountAsync(tx).ConfigureAwait(false));
                            });
                            break;

                        case QueueType.LeaseQueue:
                            totalCount = await leasedItems.GetCountAsync(tx).ConfigureAwait(false);
                            // If the leased count > 0, but the next expiration date time is DateTimeMax, then reset the next expiration.
                            if ((totalCount > 0) && (DateTimeOffset.MaxValue == _nextLeaseExpiration)) _nextLeaseExpiration = DateTimeOffset.UtcNow;
                            break;

                        case QueueType.ExpiredQueue:
                            totalCount = await expiredItems.GetCountAsync(tx).ConfigureAwait(false);
                            break;

                        case QueueType.ItemQueue:
                            totalCount = await items.GetCountAsync(tx).ConfigureAwait(false);
                            break;

                        default:
                            // Get the correct queue and query for the count.
                            var qx = await GetQueueAsync(queue).ConfigureAwait(false);
                            totalCount = await qx.GetCountAsync(tx).ConfigureAwait(false);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _eventSource.QueueMethodException(Context.PartitionId, Context.ReplicaId, ex);
                throw;
            }

            // Log success.
            sw.Stop();
            if (null != _eventSource)
            {
                _eventSource.QueueMethodLogging(nameof(CountAsync), Context.PartitionId, Context.ReplicaId, sw.ElapsedMilliseconds, $"TotalCount: {totalCount}.");
            }

            return totalCount;
        }

        /// <summary>
        /// Removes and returns the top items from the specified queues.
        /// </summary>
        /// <param name="count">Number of items to retrieve. Default is 1.</param>
        /// <param name="startQueue">First queue to dequeue from. Default is 0.</param>
        /// <param name="endQueue">Last queue to dequeue from. Default is -1, which represents the last queue.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns>List of QueueItem&lt;TItem&gt; instances.</returns>
        public async Task<IEnumerable<QueueItem<TItem>>> DequeueAsync(int count, QueueType startQueue, QueueType endQueue, CancellationToken cancellationToken)
        {
            _requestsPerSecond += 1;
            _eventSource.QueueMethodStart(Context.PartitionId, Context.ReplicaId, nameof(DequeueAsync));

            List<QueueItem<TItem>> results = new List<QueueItem<TItem>>(count);

            // Check if the start queue is < 0, if it is then start at the first queue and end at the last queue.
            if (startQueue < 0)
            {
                startQueue = 0;
                endQueue = this._queueNames.Length - 1;
            }

            // Check if the end queue is the default, if so set to the number of queues.
            if (-1 == endQueue) endQueue = _queueNames.Length - 1;

            // Validate passed arguments.
            if ((startQueue < 0) || (startQueue >= _queueNames.Length) || (startQueue > endQueue))
            {
                throw new ArgumentOutOfRangeException(nameof(startQueue), $"{startQueue} is out of range.");
            }
            if ((endQueue < 0) || (endQueue >= _queueNames.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(endQueue), $"{endQueue} is out of range.");
            }

            Stopwatch sw = Stopwatch.StartNew();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            // Get the necessary collections.
            var items = await GetItemsDictionaryAsync().ConfigureAwait(false);

            // Get the initial queue and queue count.
            int errorCount = 0;
            int queueOffset = startQueue;
            while ((results.Count < count) && (queueOffset <= endQueue) && (errorCount < 5))
            {
                // Get the queue.
                IReliableQueue<PopReceipt> q = await GetQueueAsync(queueOffset).ConfigureAwait(false);

                // TODO: Wrap in RetryPolicy instead of using errorCount -- Create a transaction.
                using (ITransaction tx = StateManager.CreateTransaction())
                {
                    try
                    { 
                        // Get a key from the queue.
                        ConditionalValue<PopReceipt> keyResult = await q.TryDequeueAsync(tx, QueueConfig.FabricOperationTimeout, cancellationToken).ConfigureAwait(false);
                        if (keyResult.HasValue)
                        {
                            // Find the item in the list of items. If the item isn't found in the items, then it must have been deleted while in the queue. 
                            // Leave the item removed from the queue as the consumer will think that this has already been removed.
                            ConditionalValue<QueueItem<TItem>> itemResult = await items.TryGetValueAsync(tx, keyResult.Value, LockMode.Update, QueueConfig.FabricOperationTimeout, cancellationToken).ConfigureAwait(false);
                            if (itemResult.HasValue)
                            {
                                // Check the expiration date. If the item is expired, remove from the item queue and place within the expired queue.
                                if (itemResult.Value.ExpirationTime < now)
                                {
                                    await ExpireItemAsync(tx, itemResult.Value, cancellationToken).ConfigureAwait(false);
                                }
                                else if (TimeSpan.Zero != itemResult.Value.LeaseDuration)
                                {
                                    // If the default lease period for the queue has been set, then lease the item. Add the item to the list of items to be returned.
                                    results.Add(await LeaseItemAsync(tx, keyResult.Value, itemResult.Value, cancellationToken).ConfigureAwait(false));
                                }

                                _avgDequeueLatency += sw.ElapsedMilliseconds;
                            }
                            else  // Log an event, but commit the transaction to remove the orphaned key in the queue otherwise forward progress will be blocked.
                            {                                
                                _eventSource.QueueItemNotPresentInItems(keyResult.Value.ToString(), Context.PartitionId, Context.ReplicaId);
                            }

                            // Commit the transaction.
                            await tx.CommitAsync().ConfigureAwait(false);
                        }
                        else  // Advance to the next queue.
                        {
                            queueOffset++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _eventSource.QueueMethodException(Context.PartitionId, Context.ReplicaId, ex);
                        errorCount++;
                        tx.Abort();
                    }
                }
            }

            // Log and return the temporary list that was committed.
            _eventSource.QueueMethodLogging(nameof(DequeueAsync), Context.PartitionId, Context.ReplicaId, sw.ElapsedMilliseconds, $"Successfully dequeued {results.Count} items with {errorCount} errors.");

            return results;
        }

        /// <summary>
        /// Enqueues a new item to the queue.
        /// </summary>
        /// <param name="itemsToEnqueue">Item to enqueue encoded as a string.</param>
        /// <param name="lease">Duration after a dequeue operation when the items lease will expire.</param>
        /// <param name="expiration">Duration after enqueue when the item expires and will not be processed.</param>
        /// <param name="queue">Destination queue. Default is queue zero.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns>List of QueueItem&lt;TItem&gt; instances.</returns>
        public async Task<IEnumerable<QueueItem<TItem>>> EnqueueAsync(IEnumerable<TItem> itemsToEnqueue, QueueType queue, TimeSpan lease, TimeSpan expiration, CancellationToken cancellationToken)
        {
            Guard.ArgumentNotNull(itemsToEnqueue, nameof(itemsToEnqueue));
            Guard.ArgumentInRange<int>(queue, QueueType.FirstQueue, _queueNames.Length - 1, nameof(queue));
            Guard.ArgumentInRange(lease, TimeSpan.Zero, TimeSpan.MaxValue, nameof(lease));
            Guard.ArgumentInRange(expiration, TimeSpan.Zero, TimeSpan.MaxValue, nameof(expiration));

            _requestsPerSecond += 1;
            _eventSource.QueueMethodStart(Context.PartitionId, Context.ReplicaId, nameof(EnqueueAsync));

            // If default lease or expiration were specified by the caller, then add the configured lease or expiration.
            lease = (default(TimeSpan) == lease) ? QueueConfig.LeaseDuration : lease;
            expiration = (default(TimeSpan) == lease) ? QueueConfig.ItemExpiration : expiration;

            try
            {
                Stopwatch sw = Stopwatch.StartNew();
                List<QueueItem<TItem>> results = new List<QueueItem<TItem>>();

                // Get the necessary collections.
                var items = await GetItemsDictionaryAsync().ConfigureAwait(false);
                var expiredItems = await GetExpiredItemsDictionaryAsync().ConfigureAwait(false);
                var leasedItems = await GetLeasedItemsDictionaryAsync().ConfigureAwait(false);
                var q = await GetQueueAsync(queue).ConfigureAwait(false);

                // Add the items to the queue.
                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    foreach (TItem item in itemsToEnqueue)
                    {
                        PopReceipt key = PopReceipt.NewPopReceipt(_service.PartitionOffset);

                        DateTimeOffset enqueueTime = DateTimeOffset.UtcNow;
                        DateTimeOffset expires = (default(TimeSpan) == expiration) ? DateTimeOffset.MaxValue : enqueueTime.Add(expiration);

                        // Wrap in a QueueItem, add to the list of items, add to the correct queue.
                        QueueItem<TItem> qi = new QueueItem<TItem>(key, queue, item, lease, DateTimeOffset.MaxValue, expires, enqueueTime, 0);

                        await items.AddAsync(tx, key, qi, QueueConfig.FabricOperationTimeout, cancellationToken).ConfigureAwait(false);
                        await q.EnqueueAsync(tx, key, QueueConfig.FabricOperationTimeout, cancellationToken).ConfigureAwait(false);

                        // Add to results list.
                        results.Add(qi);
                    }

                    // Commit the transaction and return if successful.
                    await tx.CommitAsync().ConfigureAwait(false);
                    _eventSource.QueueMethodLogging(nameof(EnqueueAsync), Context.PartitionId, Context.ReplicaId, sw.ElapsedMilliseconds, $"Successfully enqueued {results.Count} items.");
                    _avgEnqueueLatency += sw.ElapsedMilliseconds;
                }

                return results;
            }
            catch (Exception ex) { _eventSource.QueueMethodException(Context.PartitionId, Context.ReplicaId, ex); throw; }
        }

        /// <summary>
        /// Peeks at the next item without removing it from the queue.
        /// </summary>
        /// <param name="startQueue">First queue to dequeue from. Default is 0.</param>
        /// <param name="endQueue">Last queue to dequeue from. Default is -1, which represents the last queue.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns>Returns the next QueueItem&lt;TItem&gt; item to be returned.</returns>
        /// <remarks>The item is not guaranteed to be there when dequeuing, another client could have dequeued it.</remarks>
        public async Task<QueueItem<TItem>> PeekItemAsync(QueueType startQueue, QueueType endQueue, CancellationToken cancellationToken)
        {
            // Check if the start queue is < 0, if it is then start at the first queue and end at the last queue.
            if (startQueue < 0)
            {
                startQueue = 0;
                endQueue = this._queueNames.Length - 1;
            }

            // Check if the end queue is the default, if so set to the number of queues.
            if (-1 == endQueue) endQueue = this._queueNames.Length - 1;

            // Validate passed arguments.
            if ((startQueue < 0) || (startQueue >= this._queueNames.Length) || (startQueue > endQueue))
            {
                throw new ArgumentOutOfRangeException(nameof(startQueue), $"{startQueue} is out of range.");
            }
            if ((endQueue < 0) || (endQueue >= this._queueNames.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(endQueue), $"{endQueue} is out of range.");
            }

            _requestsPerSecond += 1;
            _eventSource.QueueMethodStart(Context.PartitionId, Context.ReplicaId, nameof(PeekItemAsync));

            try
            {
                Stopwatch sw = Stopwatch.StartNew();
                DateTimeOffset now = DateTimeOffset.UtcNow;
                QueueItem<TItem> item = default(QueueItem<TItem>);

                // Get the necessary collections.
                var items = await GetItemsDictionaryAsync().ConfigureAwait(false);

                using (ITransaction tx = StateManager.CreateTransaction())
                {
                    int queueOffset = startQueue;

                    // Get the initial queue and queue count.
                    IReliableQueue<PopReceipt> q = await GetQueueAsync(queueOffset).ConfigureAwait(false);

                    while (true)
                    {
                        // Get a key from the queue.
                        ConditionalValue<PopReceipt> keyResult = await q.TryPeekAsync(tx, QueueConfig.FabricOperationTimeout, cancellationToken).ConfigureAwait(false);
                        if (keyResult.HasValue)
                        {
                            // Find the item in the list of items. If the item isn't found in the items, then it must have been deleted while in the queue. 
                            // Remove the item from the queue as the consumer will think that this has already been removed.
                            ConditionalValue<QueueItem<TItem>> itemResult = await items.TryGetValueAsync(tx, keyResult.Value, LockMode.Update, QueueConfig.FabricOperationTimeout, cancellationToken).ConfigureAwait(false);
                            if ((itemResult.HasValue) && (itemResult.Value.ExpirationTime >= now))
                            {
                                item = itemResult.Value;
                                break;
                            }
                            else
                            {
                                // Dequeue the item because it was missing from the list of items and this shouldn't be queued.
                                keyResult = await q.TryDequeueAsync(tx, QueueConfig.FabricOperationTimeout, cancellationToken).ConfigureAwait(false);
                            }
                        }
                        else  // Advance to the next queue.
                        {
                            queueOffset++;
                            if (queueOffset <= endQueue)
                            {
                                q = await GetQueueAsync(queueOffset);
                            }
                            else
                            {
                                break;  // there are not more items in any of the queues.
                            }
                        }
                    }

                    // Commit the transaction and log success.
                    await tx.CommitAsync().ConfigureAwait(false);
                    _eventSource.QueueMethodLogging(nameof(PeekItemAsync), Context.PartitionId, Context.ReplicaId, sw.ElapsedMilliseconds, "Peeked complete.");

                    // Return the item.
                    return item;
                }
            }
            catch (Exception ex) { _eventSource.QueueMethodException(Context.PartitionId, Context.ReplicaId, ex); throw; }
        }

        /// <summary>
        /// Gets the list of items in the queue.
        /// </summary>
        /// <param name="top">Integer value that sets the maximum number of items to be returned. Maximum allowed is 1000.</param>
        /// <param name="skip">Integer value that sets the number of items to skip before starting to return items.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns>List of QueueItem&lt;TItem&gt; items.</returns>
        public async Task<IEnumerable<QueueItem<TItem>>> GetItemsAsync(int top, int skip, CancellationToken cancellationToken)
        {
            Guard.ArgumentInRange(top, 0, long.MaxValue, nameof(top));
            Guard.ArgumentInRange(skip, 0, long.MaxValue, nameof(skip));
            top = (top > 1000) ? 1000 : top;

            _requestsPerSecond += 1;
            _eventSource.QueueMethodStart(Context.PartitionId, Context.ReplicaId, nameof(GetItemsAsync));

            // If the user asked for no items, return the empty list.
            if (0 == top)
                return EmptyQueueItemList;

            try
            {
                int count = 0;
                Stopwatch sw = Stopwatch.StartNew();
                List<QueueItem<TItem>> results = new List<QueueItem<TItem>>(top);

                // Get the necessary collections.
                var items = await GetItemsDictionaryAsync().ConfigureAwait(false);

                // Look for the items.
                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    var enumerable = await items.CreateEnumerableAsync(tx).ConfigureAwait(false);
                    var e = enumerable.GetAsyncEnumerator();

                    // Skip the number of items requested by the 'skip' parameter.
                    while((count < skip) && await e.MoveNextAsync(cancellationToken))
                    {
                        count++;
                    }

                    // Return the next 'top' items.
                    while((results.Count < top) && await e.MoveNextAsync(cancellationToken))
                    {
                        results.Add(e.Current.Value);
                    }

                    _eventSource.QueueMethodLogging(nameof(GetItemsAsync), Context.PartitionId, Context.ReplicaId, sw.ElapsedMilliseconds, $"{count} items");
                    return results;
                }
            }
            catch (Exception ex)
            {
                _eventSource.QueueMethodException(Context.PartitionId, Context.ReplicaId, ex);
                throw;
            }
        }

        /// <summary>
        /// Peeks at the next set of item keys without removing them from the queue.
        /// </summary>
        /// <param name="queue">Queue to dequeue from. Default is 0.</param>
        /// <param name="top">Integer value that sets the maximum number of items to be returned. Maximum allowed is 1000.</param>
        /// <param name="skip">Integer value that sets the number of items to skip before starting to return items.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns>Returns the key of the next TOP items to be returned.</returns>
        /// <remarks>The item is not guaranteed to be there when dequeuing, another client could have dequeued it.</remarks>
        public async Task<IEnumerable<PopReceipt>> PeekKeysAsync(QueueType queue, int top, int skip, CancellationToken cancellationToken)
        {
            // Validate passed arguments.
            Guard.ArgumentInRange(top, 0, long.MaxValue, nameof(top));
            Guard.ArgumentInRange(skip, 0, long.MaxValue, nameof(skip));
            if ((queue < 0) || (queue >= this._queueNames.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(queue), $"{queue} is out of range.");
            }

            // If top is greater than 1000, change to 1000.
            top = (top > 1000) ? 1000 : top;

            _requestsPerSecond += 1;
            _eventSource.QueueMethodStart(Context.PartitionId, Context.ReplicaId, nameof(PeekItemAsync));

            try
            {
                Stopwatch sw = Stopwatch.StartNew();
                DateTimeOffset now = DateTimeOffset.UtcNow;
                List<PopReceipt> returnKeys = new List<PopReceipt>(top);

                // Get the necessary queue.
                IReliableQueue<PopReceipt> q = await GetQueueAsync(queue).ConfigureAwait(false);

                using (ITransaction tx = StateManager.CreateTransaction())
                {
                    // Get the async enumerator.
                    IAsyncEnumerable<PopReceipt> keyEnumerator = await q.CreateEnumerableAsync(tx).ConfigureAwait(false);
                    var keys = keyEnumerator.GetAsyncEnumerator();

                    while (await keys.MoveNextAsync(cancellationToken).ConfigureAwait(false) && (returnKeys.Count < top))
                    {
                        // If we there are items to skip, move to the next item.
                        if (skip-- > 0)
                            continue;

                        returnKeys.Add(keys.Current);
                    }

                    // Commit the transaction and log success.
                    await tx.CommitAsync().ConfigureAwait(false);
                    _eventSource.QueueMethodLogging(nameof(PeekKeysAsync), Context.PartitionId, Context.ReplicaId, sw.ElapsedMilliseconds, $"PeekKeysAsync complete in {sw.ElapsedMilliseconds}ms. {returnKeys.Count} keys returned.");

                    // Return the item.
                    return returnKeys;
                }
            }
            catch (Exception ex)
            {
                _eventSource.QueueMethodException(Context.PartitionId, Context.ReplicaId, ex);
                return new List<PopReceipt>();
            }
        }

        /// <summary>
        /// Delete an item from the queue.
        /// </summary>
        /// <param name="key">Array of PopReceipt keys.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns>List of QueueItem&lt;TItem&gt; items.</returns>
        public async Task<QueueItem<TItem>> DeleteItemAsync(PopReceipt key, CancellationToken cancellationToken)
        {
            Guard.ArgumentNotNull(key, nameof(key));

            _requestsPerSecond += 1;
            _eventSource.QueueMethodStart(Context.PartitionId, Context.ReplicaId, nameof(DeleteItemAsync));

            Stopwatch sw = Stopwatch.StartNew();
        
            QueueItem<TItem> item = default(QueueItem<TItem>);

            try
            {
                // Get the necessary collections.
                var items = await GetItemsDictionaryAsync().ConfigureAwait(false);
                var leasedItems = await GetLeasedItemsDictionaryAsync().ConfigureAwait(false);
                var expiredItems = await GetExpiredItemsDictionaryAsync().ConfigureAwait(false);

                // Look for the items.
                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    // Try to get the item from the list of items.
                    ConditionalValue<QueueItem<TItem>> result = await items.TryRemoveAsync(tx, key).ConfigureAwait(false);
                    if (result.HasValue)
                    {
                        // If it's leased, remove it from the list of leased items.
                        if (QueueType.LeaseQueue == result.Value.Queue)
                        {
                            await leasedItems.TryRemoveAsync(tx, key);
                        }
                        else if (QueueType.ExpiredQueue == result.Value.Queue) // and if expired, remove it from the list of expired items.
                        {
                            await expiredItems.TryRemoveAsync(tx, key);
                        }

                        item = result.Value;
                    }

                    // Commit the transaction and return if successful.
                    await tx.CommitAsync().ConfigureAwait(false);
                    _eventSource.QueueMethodLogging(nameof(DeleteItemAsync), Context.PartitionId, Context.ReplicaId, sw.ElapsedMilliseconds, $"Successfully removed {key}.");
                }

                return item;
            }
            catch (Exception ex)
            {
                _eventSource.QueueMethodException(Context.PartitionId, Context.ReplicaId, ex);
                throw;
            }
        }

        /// <summary>
        /// Update the state of a set of leased items. The value of lease replaces the existing lease time.
        /// </summary>
        /// <param name="keys">Array of item keys to update if present.</param>
        /// <param name="lease">New lease duration from now. If TimeSpan.Zero is passed, the lease will be released.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns>Boolean array of indicators of success or failure.</returns>
        /// <remarks>If the value of lease is zero or negative, the item will be removed from the leased items and not placed back into a queue. 
        /// If the value is positive, the lease will expire that time interval from now.</remarks>
        public async Task<IEnumerable<bool>> ExtendLeaseAsync(IEnumerable<PopReceipt> keys, TimeSpan lease, CancellationToken cancellationToken)
        {
            Guard.ArgumentNotNull(keys, nameof(keys));
            Guard.ArgumentInRange(lease, TimeSpan.Zero, TimeSpan.MaxValue, nameof(lease));

            _requestsPerSecond += 1;
            _eventSource.QueueMethodStart(Context.PartitionId, Context.ReplicaId, nameof(ExtendLeaseAsync), lease.ToString());

            IEnumerable<bool> results = EmptyBooleanList;

            try
            {
                Stopwatch sw = Stopwatch.StartNew();

                using (ITransaction tx = StateManager.CreateTransaction())
                {
                    // If the lease time is greater than zero, update the lease time.
                    if (lease > TimeSpan.Zero)
                    {
                        results = await UpdateItemsLeaseAsync(tx, keys, lease, cancellationToken).ConfigureAwait(false);
                    }
                    else // the new lease value is zero or negative, remove the item if present.
                    {
                        results = await RemoveLeasedItemsAsync(tx, keys, cancellationToken).ConfigureAwait(false);
                    }

                    // Commit the transaction.
                    await tx.CommitAsync().ConfigureAwait(false);

                    int failedCount = results.Count(value => value == false);
                    _eventSource.QueueMethodLogging(nameof(ExtendLeaseAsync), Context.PartitionId, Context.ReplicaId, sw.ElapsedMilliseconds, $"{failedCount} of {results.Count()} failed.");
                    _avgExtendLeaseLatency += sw.ElapsedMilliseconds;

                    return results;
                }
            }
            catch (Exception ex)
            {
                _eventSource.QueueMethodException(Context.PartitionId, Context.ReplicaId, ex);
                throw;
            }
        }

        /// <summary>
        /// Releases the lease of a queue item and removes it from the list of leased items.
        /// </summary>
        /// <param name="keys">Keys of the items to release.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns>Boolean indicator of success or failure.</returns>
        public Task<IEnumerable<bool>> ReleaseLeaseAsync(IEnumerable<PopReceipt> keys, CancellationToken cancellationToken)
        {
            _eventSource.QueueMethodStart(Context.PartitionId, Context.ReplicaId, nameof(ReleaseLeaseAsync));
            return ExtendLeaseAsync(keys, TimeSpan.Zero, cancellationToken);
        }

        #endregion

        #region IDisposable Support

        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _leaseTimer?.Dispose();
                    _healthTimer?.Dispose();
                }

                _leaseTimer = null;
                _healthTimer = null;

                disposedValue = true;
            }
        }

        /// <summary>
        /// This code added to correctly implement the disposable pattern. 
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        #endregion
    }
}
