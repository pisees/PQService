// ------------------------------------------------------------
//  <copyright file="HttpQueueClient.cs" Company="Microsoft Corporation">
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// </copyright>
// ------------------------------------------------------------

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("QueueServiceTests")]

namespace QuickService.QueueClient
{
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Communication.Client;
    using Common;
    using Common.ErrorHandling;
    using Common.Rest;
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Http;
    using Newtonsoft.Json;
    using System.Text;
    using System.Net;

    /// <summary>
    /// Queue Client
    /// </summary>
    /// <typeparam name="TItem">Type of the item being stored in the queue.</typeparam>
    public sealed class HttpQueueClient<TItem> : IDisposable
        where TItem : IEquatable<TItem>
    {
        /// <summary>
        /// Custom header value for a client generated request identifier.
        /// </summary>
        const string c_ClientRequestId = "x-ms-client-request-id";

        #region Properties

        /// <summary>
        /// Static empty list of QueueItem&lt;TItem&gt;.
        /// </summary>
        private static IEnumerable<QueueItem<TItem>> EmptyQueueItemList = new List<QueueItem<TItem>>();

        /// <summary>
        /// Static empty list of Boolean values.
        /// </summary>
        private static IEnumerable<bool> EmptyBooleanList = new List<bool>();

        /// <summary>
        /// HttpClient instance.
        /// </summary>
        private HttpClient _http = null;

        /// <summary>
        /// Retry policy instance.
        /// </summary>
        private RetryPolicy _retryPolicy = null;

        /// <summary>
        /// The next partition that will be serviced.
        /// </summary>
        private long _currentDequeuePartition = 0;

        /// <summary>
        /// The next partition items will be sent to.
        /// </summary>
        private long currentEnqueuePartition = 0;

        /// <summary>
        /// Uri for the service.
        /// </summary>
        private readonly Uri _serviceUri;

        /// <summary>
        /// Gets the name of the listener.
        /// </summary>
        private readonly string _listenerName;

        /// <summary>
        /// Operation timeout period. Default is 4 seconds.
        /// </summary>
        private TimeSpan OperationTimeout = TimeSpan.FromSeconds(4);

        /// <summary>
        /// Gets the number of partitions configured for this queue service.
        /// </summary>
        public int ServicePartitionCount { get; private set; }

        /// <summary>
        /// Maps the partition number (0..N, not the key value) to a ResolvedServicePartition.
        /// </summary>
        private Tuple<ServicePartitionKey, ResolvedServicePartition>[] _partitionMap;

        #endregion

        #region Constructors

        /// <summary>
        /// QueueClient constructor.
        /// </summary>
        /// <param name="serviceUri">Uri of the service in the format fabric:/[app]/[svc].</param>
        /// <param name="listenerName">String containing the name of the listener. Default to 'SvcEndpoint'</param>
        /// <param name="retryPolicy">RetryPolocy instance. Defaults to <see name="RetryPolicy"/>.DefaultFixed.</param>
        /// <remarks>There is quite a bit of overhead to creating an instance of QueueClient. Create one per service and reuse.</remarks>
        public HttpQueueClient(Uri serviceUri, string listenerName = "SvcEndpoint", RetryPolicy retryPolicy = null)
        {
            Guard.ArgumentNotNull(serviceUri, nameof(serviceUri));

            _serviceUri = serviceUri;
            _listenerName = listenerName;

            // Create the retry policy
            _retryPolicy = retryPolicy ?? RetryPolicy.DefaultFixed;

            // 
            var handler = new HttpClientHandler()
            {
                ClientCertificateOptions = ClientCertificateOption.Automatic
            };

            // Initialize the HttpClient and the ServiceFabricEndpointResolver.
            _http = new HttpClient(handler);
            //_endpointResolver = new PartitionEndpointResolver(clusterEndpoint, TimeSpan.FromMinutes(10), _http);

            // Get the number of partitions configured for this service.
            ServicePartitionCount = ClusterHelpers.GetPartitionCountAsync(serviceUri).GetAwaiter().GetResult();
            Guard.ArgumentNotZeroOrNegativeValue(ServicePartitionCount, nameof(ServicePartitionCount));

            // Allocate and initialize the partition map, which caches ResolvedServicePartition instances.
            // The map caches the ResolvedServicePartition and also the ServicePartitionKey so it doesn't have to be created for each call.
            _partitionMap = new Tuple<ServicePartitionKey, ResolvedServicePartition>[ServicePartitionCount];
            for (int i = 0; i < ServicePartitionCount; i++)
            {
                _partitionMap[i] = new Tuple<ServicePartitionKey, ResolvedServicePartition>(new ServicePartitionKey((long)i), null);
            }

            // Choose a random partition to start with
            Guard.ArgumentInRange(ServicePartitionCount, 1, int.MaxValue, nameof(ServicePartitionCount));
            _currentDequeuePartition = RandomThreadSafe.Instance.Next(0, ServicePartitionCount - 1);
            currentEnqueuePartition = RandomThreadSafe.Instance.Next(0, ServicePartitionCount - 1);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the next partition to service.
        /// </summary>
        /// <returns>Partition number.</returns>
        /// <remarks>It's OK if two threads get the same partition number infrequently to avoid longer locking.</remarks>
        private long NextDequeuePartition()
        {
            long value = Interlocked.Increment(ref _currentDequeuePartition);
            if (value >= ServicePartitionCount)
            {
                Interlocked.Exchange(ref _currentDequeuePartition, 0);
                value = 0;
            }

            return value;
        }

        /// <summary>
        /// Gets the next partition to enqueue to.
        /// </summary>
        /// <returns>Partition number.</returns>
        /// <remarks>It's OK if two threads get the same partition number infrequently to avoid longer locking.</remarks>
        private long NextEnqueuePartition()
        {
            long value = Interlocked.Increment(ref currentEnqueuePartition);
            if (value >= ServicePartitionCount)
            {
                Interlocked.Exchange(ref currentEnqueuePartition, 0);
                value = 0;
            }

            return value;
        }

        /// <summary>
        /// Gets the partition and ensures all items in the list are within the same partition.
        /// </summary>
        /// <param name="keys">IEnumerable of PopReceipt keys to evaluate.</param>
        /// <returns>Long integer containing the partition number between 0 and the number of partitions minus one or -1 if the partitions are not the same.</returns>
        private long GetPartitionAndEnsureSame(IEnumerable<PopReceipt> keys)
        {
            long partition = -1;

            // Check that all keys are within the same partition.
            foreach (PopReceipt pr in keys)
            {
                if (-1 == partition)
                    partition = pr.Partition;
                else if (partition != pr.Partition)
                    return -1;
            }

            return partition;
        }

        /// <summary>
        /// Gets the ResolvedServicePartition for a partition key value.
        /// </summary>
        /// <param name="partition">Partition value.</param>
        /// <param name="token">CancellationToken instance.</param>
        /// <returns>ResolvedServicePartition instance or null if resolution is not possible.</returns>
        private async Task<ResolvedServicePartition> GetRspAsync(long partition, CancellationToken token)
        {
            ServicePartitionResolver resolver = ServicePartitionResolver.GetDefault();

            // Get the ResolvedServicePartition for this partition.
            ResolvedServicePartition rsp = _partitionMap[partition].Item2;
            if (null == rsp)
            {
                var spk = new ServicePartitionKey(partition);
                rsp = await resolver.ResolveAsync(_serviceUri, spk, token).ConfigureAwait(false);
                _partitionMap[partition] = new Tuple<ServicePartitionKey, ResolvedServicePartition>(spk, rsp);
            }

            return rsp;
        }

        /// <summary>
        /// Calls a service endpoint with retries.
        /// </summary>
        /// <typeparam name="TResult">Type of the result.</typeparam>
        /// <param name="rsp">ResolvedServicePartition instance.</param>
        /// <param name="token">CancellationToken instance.</param>
        /// <param name="func">Fucntion to execute, actually makeing the HTTP request.</param>
        /// <returns>Result of the function execution.</returns>
        private async Task<TResult> CallAsync<TResult>(ResolvedServicePartition rsp, CancellationToken token, Func<ResolvedServiceEndpoint, CancellationToken, Task<TResult>> func)
        {
            TResult result = default(TResult);

            // Wrap in a retry policy.
            await _retryPolicy.ExecuteWithRetriesAsync(async (ct) =>
            {
                try
                {
                    ResolvedServiceEndpoint rse = rsp.GetEndpoint();
                    result = await func(rse, ct);
                }
                catch (FabricTransientException)
                {
                    rsp = await GetRspAsync(((Int64RangePartitionInformation)rsp.Info).LowKey, token).ConfigureAwait(false);
                }
                catch (HttpRequestException ex) when ((ex.InnerException as WebException)?.Status == WebExceptionStatus.ConnectFailure)
                {
                    rsp = await GetRspAsync(((Int64RangePartitionInformation)rsp.Info).LowKey, token).ConfigureAwait(false);
                }
            }, cancellationToken: token);

            return result;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Retrieves the number of queue priorities.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken instance used to cancel this operation.</param>
        /// <returns>Integer indicating the number priorities or -1 if an error occurred.</returns>
        public async Task<int> PriorityCountAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            int count = -1;

            string activityId = Guid.NewGuid().ToString();
            string relativeUri = $"/api/prioritycount?requestid={activityId}";

            // Get the Resolved ServicePartition for the specified partition.
            ResolvedServicePartition rsp = await GetRspAsync(0, cancellationToken).ConfigureAwait(false);

            // Call to the service endpoint. CallAsync will retry and handle moving endpoints.
            count = await CallAsync(rsp, cancellationToken, async (serviceEndpoints, ct) =>
            {
                string uri = serviceEndpoints.GetFirstEndpoint() + relativeUri;
                using (HttpResponseMessage result = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(false))
                {
                    if (result.IsSuccessStatusCode)
                    {
                        return int.Parse(await result.Content.ReadAsStringAsync().ConfigureAwait(false));
                    }

                    // TODO: Replace with specific exception rather than this one.
                    throw new HttpRequestException(result.ReasonPhrase, new WebException(result.ReasonPhrase, WebExceptionStatus.ConnectFailure));
                }
            });

            return count;
        }

        /// <summary>
        /// Retrieves the number of items in the specified queue.
        /// </summary>
        /// <param name="queue">Integer value indicating the queue.</param>
        /// <param name="cancellationToken">CancellationToken instance used to cancel this operation.</param>
        /// <returns>Long integer indicating the number of items in the specified queue or -1 if an error occurred.</returns>
        public async Task<long> CountAsync(int queue = QueueType.AllQueues, CancellationToken cancellationToken = default(CancellationToken))
        {
            long totalCount = 0;
            int successCount = 0;

            string activityId = Guid.NewGuid().ToString();
            string relativeUri = $"/api/count?queue={queue}&requestid={activityId}";

            for (long partition = 0; partition < ServicePartitionCount; partition++)
            {
                // Get the Resolved ServicePartition for the specified partition.
                ResolvedServicePartition rsp = await GetRspAsync(partition, cancellationToken).ConfigureAwait(false);

                long count = await CallAsync(rsp, cancellationToken, async (serviceEndpoints, ct) =>
                {
                    string uri = serviceEndpoints.GetFirstEndpoint() + relativeUri;
                    using (HttpResponseMessage result = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(false))
                    {
                        if (result.IsSuccessStatusCode)
                        {
                            Interlocked.Increment(ref successCount);
                            int value = int.Parse(await result.Content.ReadAsStringAsync().ConfigureAwait(false));

                            //Debug.WriteLine($"Queue {queue} Endpoint: {uri} Partition: {partition} Code: {result.StatusCode} Value: {value}");
                            return value;
                        }

                        // TODO: Replace with specific exception rather than this one.
                        throw new HttpRequestException(result.ReasonPhrase, new WebException(result.ReasonPhrase, WebExceptionStatus.ConnectFailure));
                    }
                });

                // Add the value from this partition to the total.
                Interlocked.Add(ref totalCount, count);
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
        /// <param name="activityId">Activity identifier. If one is not provided a new one will be generated.</param>
        /// <returns>List of QueueItem&lt;TItem&gt; instances.</returns>
        /// <seealso cref="QueueItem{TItem}"/>
        public async Task<IEnumerable<QueueItem<TItem>>> DequeueAsync(int count = 1, 
            int startQueue = 
            QueueType.FirstQueue, 
            int endQueue = QueueType.LastQueue, 
            CancellationToken cancellationToken = default(CancellationToken),
            string activityId = null)
        {
            Guard.ArgumentInRange(count, 1, 1000, nameof(count));

            IEnumerable<QueueItem<TItem>> list = null;

            // Get the next partition to add the items to, this is a simple round robin from 0 to number of partitions.
            long partition = NextDequeuePartition();
            activityId = activityId ?? Guid.NewGuid().ToString();
            string relativeUri = $"/api?count={count}&startqueue={startQueue}&endqueue={endQueue}&requestid={activityId}";

            // Get the Resolved ServicePartition for the specified partition.
            ResolvedServicePartition rsp = await GetRspAsync(partition, cancellationToken).ConfigureAwait(false);

            // Make the HTTP request. CallAsync handles retries and moving endpoints.
            list = await CallAsync(rsp, cancellationToken, async (serviceEndpoints, ct) =>
            {
                string uri = serviceEndpoints.GetFirstEndpoint() + relativeUri;
                using (HttpResponseMessage result = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(false))
                {
                    if (result.IsSuccessStatusCode)
                    {
                        return result.GetResult<IEnumerable<QueueItem<TItem>>>();
                    }

                    // TODO: Replace with specific exception rather than this one.
                    throw new HttpRequestException(result.ReasonPhrase, new WebException(result.ReasonPhrase, WebExceptionStatus.ConnectFailure));
                }
            });

            // Return the list.
            return list;
        }

        /// <summary>
        /// Enqueues a new item to the queue.
        /// </summary>
        /// <param name="items">Items of type <typeparamref name="TItem"/> to enqueue.</param>
        /// <param name="queue">Destination queue. Default is queue zero.</param>
        /// <param name="lease">Duration after a dequeue operation when the items lease will expire.</param>
        /// <param name="expiration">Duration after enqueue when the item expires and will not be processed.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <param name="activityId">Activity identifier. If one is not provided a new one will be generated.</param>
        /// <returns>List containing the enqueued QueueItem&lt;TItem&gt;.</returns>
        /// <remarks>The list of QueueItem&lt;TItem&gt; are returned in case the querying of the status of an item is desired.</remarks>
        public async Task<IEnumerable<QueueItem<TItem>>> EnqueueAsync(IEnumerable<TItem> items, 
            int queue = QueueType.FirstQueue, 
            TimeSpan lease = default(TimeSpan), 
            TimeSpan expiration = default(TimeSpan), 
            CancellationToken cancellationToken = default(CancellationToken),
            string activityId = null)
        {
            Guard.ArgumentNotNull(items, nameof(items));
            
            IEnumerable<QueueItem<TItem>> list = null;

            // Get the next partition to add the items to, this is a simple round robin from 0 to number of partitions.
            long partition = NextEnqueuePartition();
            string queryParams = activityId ?? Guid.NewGuid().ToString();

            // Parse the optional time based parameters
            if ((default(TimeSpan) != lease) && (default(TimeSpan) != expiration))
            {
                queryParams = $"{queryParams}&leaseSeconds ={lease}&expirationMinutes={expiration}";
            }
            else if (default(TimeSpan) != lease)
            {
                queryParams = $"{queryParams}&leaseSeconds={lease}";
            }
            else if (default(TimeSpan) != lease)
            {
                queryParams = $"{queryParams}&expirationMinutes={expiration}";
            }

            // Build part of the uri.
            string relativeUri = $"/api/{queue}?requestid={queryParams}";

            // Create the content.
            string json = JsonConvert.SerializeObject(items);

            // Get the Resolved ServicePartition for the specified partition.
            ResolvedServicePartition rsp = await GetRspAsync(partition, cancellationToken).ConfigureAwait(false);

            // Make the HTTP request. CallAsync handles retries and moving endpoints.
            list = await CallAsync(rsp, cancellationToken, async (serviceEndpoints, ct) =>
            {
                string uri = serviceEndpoints.GetFirstEndpoint() + relativeUri;
                using (HttpContent content = new StringContent(json, Encoding.UTF8, "application/json"))
                using (HttpResponseMessage result = await _http.PostAsync(uri, content, cancellationToken).ConfigureAwait(false))
                {
                    if (result.IsSuccessStatusCode)
                    {
                        return result.GetResult<IEnumerable<QueueItem<TItem>>>();
                    }

                    // TODO: Replace with specific exception rather than this one.
                    throw new HttpRequestException(result.ReasonPhrase, new WebException(result.ReasonPhrase, WebExceptionStatus.ConnectFailure));
                }
            });

            // Return the list.
            return list;
        }

        /// <summary>
        /// Gets a list of items not removing them from the queue.
        /// </summary>
        /// <param name="partition">Partition to query.</param>
        /// <param name="top">Number of items to return.</param>
        /// <param name="skip">Number of items to skip before returning. This allows paging of items.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <param name="activityId">Activity identifier. If one is not provided a new one will be generated.</param>
        /// <returns>List of QueueItem&lt;TItem&gt; items.</returns>
        /// <remarks>Returns the item regardless of being queued, leased or expired.</remarks>
        public async Task<IEnumerable<QueueItem<TItem>>> GetItemsAsync(long partition, int top = 1000, int skip = 0, CancellationToken cancellationToken = default(CancellationToken), string activityId = null)
        {
            Guard.ArgumentInRange(partition, 0, ServicePartitionCount - 1, nameof(partition));
            Guard.ArgumentInRange(top, 1, 1000, nameof(top));
            Guard.ArgumentInRange(skip, 0, int.MaxValue, nameof(skip));

            IEnumerable<QueueItem<TItem>> list = null;

            // Get the items from the specified partition.
            activityId = activityId ?? Guid.NewGuid().ToString();
            string relativeUri = $"/api/items?top={top}&skip={skip}&requestid={activityId}";

            // Get the Resolved ServicePartition for the specified partition.
            ResolvedServicePartition rsp = await GetRspAsync(partition, cancellationToken).ConfigureAwait(false);

            // Make the HTTP request. CallAsync handles retries and moving endpoints.
            list = await CallAsync(rsp, cancellationToken, async (serviceEndpoints, ct) =>
            {
                string uri = serviceEndpoints.GetFirstEndpoint() + relativeUri;
                using (HttpResponseMessage result = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(false))
                {
                    if (result.IsSuccessStatusCode)
                    {
                        return result.GetResult<IEnumerable<QueueItem<TItem>>>();
                    }

                    // TODO: Replace with specific exception rather than this one.
                    throw new HttpRequestException(result.ReasonPhrase, new WebException(result.ReasonPhrase, WebExceptionStatus.ConnectFailure));
                }
            });

            // Return the list.
            return list ?? EmptyQueueItemList;
        }

        /// <summary>
        /// Peeks at next item without removing it from the queue.
        /// </summary>
        /// <param name="startQueue">First queue to dequeue from. Default is 0.</param>
        /// <param name="endQueue">Last queue to dequeue from. Default is -1, which represents the last queue.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <param name="activityId">Activity identifier. If one is not provided a new one will be generated.</param>
        /// <returns>QueueItem&lt;TItem&gt; instance.</returns>
        /// <remarks>If the client is using multiple threads or there are multiple clients, it is likely that a different thread or client 
        /// will consume this item between when it is peeked and the next operation. Do not count on this item being there for the next operation.</remarks>
        public async Task<QueueItem<TItem>> PeekItemAsync(int startQueue = QueueType.FirstQueue, int endQueue = QueueType.LastQueue, 
            CancellationToken cancellationToken = default(CancellationToken), 
            string activityId = null)
        {
            // Get the partition of the first item, use the next dequeue partition, but don't advance to the next partition.
            long partition = ((_currentDequeuePartition + 1) >= ServicePartitionCount) ? 0 : _currentDequeuePartition + 1;

            QueueItem<TItem> item = default(QueueItem<TItem>);

            // Get the items from the specified partition.
            activityId = activityId ?? Guid.NewGuid().ToString();
            string relativeUri = $"/api/peek?startqueue={startQueue}&endqueue={endQueue}&requestid={activityId}";

            // Get the Resolved ServicePartition for the specified partition.
            ResolvedServicePartition rsp = await GetRspAsync(partition, cancellationToken).ConfigureAwait(false);

            // Make the HTTP request. CallAsync handles retries and moving endpoints.
            item = await CallAsync(rsp, cancellationToken, async (serviceEndpoints, ct) =>
            {
                string uri = serviceEndpoints.GetFirstEndpoint() + relativeUri;
                using (HttpResponseMessage result = await _http.GetAsync(uri, cancellationToken).ConfigureAwait(false))
                {
                    if (result.IsSuccessStatusCode)
                    {
                        return result.GetResult<QueueItem<TItem>>();
                    }

                    // TODO: Replace with specific exception rather than this one.
                    throw new HttpRequestException(result.ReasonPhrase, new WebException(result.ReasonPhrase, WebExceptionStatus.ConnectFailure));
                }
            });

            return item;
        }

        /// <summary>
        /// Delete an item from the queue.
        /// </summary>
        /// <param name="key">PopReceipt key.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns>QueueItem&lt;TItem&gt; instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="ArgumentException">All of the PopReceit instances in <paramref name="key"/> must reside within the same partition.</exception>
        public async Task<QueueItem<TItem>> DeleteItemAsync(PopReceipt key, CancellationToken cancellationToken)
        {
            QueueItem<TItem> item = default(QueueItem<TItem>);

            // Build the Uri and query parameters.
            string activityId = Guid.NewGuid().ToString();
            string relativeUri = $"/api/{key}?requestid={activityId}";

            // Get the Resolved ServicePartition for the specified partition.
            ResolvedServicePartition rsp = await GetRspAsync(key.Partition, cancellationToken).ConfigureAwait(false);

            // Make the DELETE request to the service. CallAsync handles retries and moving endpoints.
            item = await CallAsync(rsp, cancellationToken, async (serviceEndpoints, ct) =>
            {
                string uri = serviceEndpoints.GetFirstEndpoint() + relativeUri;
                using (HttpResponseMessage result = await _http.DeleteAsync(uri, cancellationToken).ConfigureAwait(false))
                { 
                    if (result.IsSuccessStatusCode)
                    {
                        return result.GetResult<QueueItem<TItem>>();
                    }

                    // TODO: Replace with specific exception rather than this one.
                    throw new HttpRequestException(result.ReasonPhrase, new WebException(result.ReasonPhrase, WebExceptionStatus.ConnectFailure));
                }
            });

            // Return the item.
            return item;
        }

        /// <summary>
        /// Update the state of a set of leased items. The value of lease replaces the existing lease time.
        /// </summary>
        /// <param name="keys">Array of item keys to update if present. All keys must belong to a single partition.</param>
        /// <param name="lease">New lease duration from now. If TimeSpan.Zero is passed, the lease will be released.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <param name="activityId">Activity identifier. If one is not provided a new one will be generated.</param>
        /// <returns>Boolean array of indicators of success or failure for each key.</returns>
        /// <remarks>If the value of lease is zero or negative, the item will be removed from the leased items and not placed back into a queue. 
        /// If the value is positive, the lease will expire that time interval from now.</remarks>
        /// <exception cref="InvalidOperationException"><paramref name="keys"/> contains no PopReceipt instances.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="keys"/> is null.</exception>
        /// <exception cref="ArgumentException">All of the PopReceit instances in <paramref name="keys"/> must reside within the same partition.</exception>
        public async Task<IEnumerable<bool>> ExtendLeaseAsync(IEnumerable<PopReceipt> keys, TimeSpan lease, CancellationToken cancellationToken, string activityId = null)
        {
            Guard.ArgumentNotNull(keys, nameof(keys));
            Guard.ArgumentInRange(lease, TimeSpan.Zero, TimeSpan.MaxValue, nameof(lease));

            // Get the partition for the set of items.
            long partition = GetPartitionAndEnsureSame(keys);
            if ((partition < 0) || (partition >= ServicePartitionCount))
                throw new ArgumentException("The list of keys must contain only keys within a single partition");

            IEnumerable<bool> list = null;

            // Get the items from the specified partition.
            activityId = activityId ?? Guid.NewGuid().ToString();
            string relativeUri = $"/api?leaseSeconds={lease.TotalSeconds}&requestid={activityId}";

            // Create the content.
            string json = JsonConvert.SerializeObject(keys);

            // Get the Resolved ServicePartition for the specified partition.
            ResolvedServicePartition rsp = await GetRspAsync(partition, cancellationToken).ConfigureAwait(false);

            // Make the request to the service. CallAsync handles retries and moving endpoints.
            list = await CallAsync(rsp, cancellationToken, async (serviceEndpoints, ct) =>
            {
                string uri = serviceEndpoints.GetFirstEndpoint() + relativeUri;
                using (HttpContent content = new StringContent(json, Encoding.UTF8, "application/json"))
                using (HttpResponseMessage result = await _http.PutAsync(uri, content, cancellationToken).ConfigureAwait(false))
                {
                    if (result.IsSuccessStatusCode)
                    {
                        return result.GetResult<IEnumerable<bool>>();
                    }

                    // TODO: Replace with specific exception rather than this one.
                    throw new HttpRequestException(result.ReasonPhrase, new WebException(result.ReasonPhrase, WebExceptionStatus.ConnectFailure));
                }
            });

            return list;
        }

        /// <summary>
        /// Releases the lease of a queue item and removes it from the list of leased items.
        /// </summary>
        /// <param name="keys">Keys of the items to release.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns>Boolean indicator of success or failure.</returns>
        public Task<IEnumerable<bool>> ReleaseLeaseAsync(IEnumerable<PopReceipt> keys, CancellationToken cancellationToken)
        {
            return ExtendLeaseAsync(keys, TimeSpan.Zero, cancellationToken);
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// To detect redundant calls.
        /// </summary>
        private bool disposedValue = false;

        /// <summary>
        /// Protected implementation of the dispose pattern.
        /// </summary>
        /// <remarks>There is currently nothing to dispose, but in case a disposable resource is added later, client code will already be written correctly.</remarks>
        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _http.Dispose();
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <remarks>There is currently nothing to dispose, but in case a disposable resource is added later, client code will already be written correctly.</remarks>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
 }
