// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using QuickService.Common;
using System.Collections.Generic;
using QuickService.QueueClient;
using QuickService.QueueService;

namespace QuickService.PriorityQueueService.Controllers
{
    /// <summary>
    /// Priority queue service controller.
    /// </summary>
    [RoutePrefix("api")]
    public sealed class DefaultController : ApiController
    {
        /// <summary>
        /// Reference to the IPriorityQueueService implementation.
        /// </summary>
        private readonly QueuePartitionOperations<Item, PriorityQueueServiceConfiguration> _qpo;

        /// <summary>
        /// Default controller constructor.
        /// </summary>
        /// <param name="qpo">QueuePartitionOperation instance.</param>
        public DefaultController(QueuePartitionOperations<Item, PriorityQueueServiceConfiguration> qpo)
        {
            //Contract.
            Guard.ArgumentNotNull(qpo, nameof(qpo));
            _qpo = qpo;
        }

        /// <summary>
        /// REST endpoint that gets the number of priorities for this queue.
        /// </summary>
        /// <returns>
        ///     HttpStatusCode.OK (200) with number of priorities configured for this queue service.
        /// </returns>
        /// <example>
        /// Examples:
        ///     Get the number of configured priorities       - /api/prioritycount
        /// </example>
        [HttpGet]
        [Route(@"prioritycount", Name = "PriorityCount")]
        public HttpResponseMessage PriorityCount()
        {
            int count = _qpo.QueueCount();
            return Request.CreateResponse(HttpStatusCode.OK, count);
        }

        /// <summary>
        /// REST endpoint that gets the number of items contained in one of the queues.
        /// </summary>
        /// <param name="queue">Queue to return the count for. Constant values defined within <see cref="QueueType"/></param>
        /// <returns>
        /// HttpStatusCode.OK (200) with number of items for the specified queue.
        /// HttpStatusCode.BadRequest (400) if the queue asked for is not within the valid range.
        /// HttpStatisCode.InternalServerError (500) an unknown error has occurred.</returns>
        /// <example>
        /// Examples:
        ///     Get the number of items waiting in a queue for all priorities       - /api/count  | /api/count?queue=-1
        ///     Get the number of leased items                                      - /api/count?queue=-2
        ///     Get the number of expired and poison items                          - /api/count?queue=-3
        ///     Get the number of items within this partition                       - /api/count?queue=-4
        ///     Get the number of items waiting in priority 0 queue                 - /api/count?queue=0
        ///     Get the number of items waiting in priority 16 queue                - /api/count?queue=16
        /// </example>
        [HttpGet]
        [Route(@"count", Name = "Count")]
        public async Task<HttpResponseMessage> CountAsync([FromUri] int queue = QueueType.AllQueues)
        {
            try
            {
                long count = await _qpo.CountAsync(queue, CancellationToken.None);
                return Request.CreateResponse(HttpStatusCode.OK, count);
            }
            catch(ArgumentOutOfRangeException ex) { return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex); }
        }

        /// <summary>
        /// REST endpoint that dequeues items from the PriorityQueueService.
        /// </summary>
        /// <param name="count">Number of items to dequeue. Default is 1, maximum is set by PriorityQueueService.MaximumDequeueCount.</param>
        /// <param name="startqueue">First queue to remove items from. Default to queue 0. Minimum value is 0, maximum value is the number of queues - 1.</param>
        /// <param name="endqueue">Last queue to removed items from. Default is -1, which indicates the maximum queue value.</param>
        /// <returns>HttpStatusCode.OK (200) with the number of QueuedItems&lt;MySample&gt; instances containing the returned items requested from one or more priorities from the queue or all of the remaining items if there are fewer than count items.
        /// HttpStatusCode.BadRequest (400) if the queue asked for is not within the valid range.
        /// HttpStatisCode.InternalServerError (500) an unknown error has occurred.</returns>
        /// <example>
        /// Examples:
        ///     Dequeue a single item from the highest priority queue containing an item       - /api
        ///     Dequeue a single item from the priority 7 or lower queue containing an item    - /api?startqueue=7
        ///     Dequeue a 10 items from the priority 2 or 3 queues if they contain an item     - /api?count=10&amp;startqueue=2&amp;endqueue=3
        /// </example>
        [HttpGet]
        [Route(@"", Name = "Dequeue")]
        public async Task<HttpResponseMessage> DequeueAsync([FromUri] int count = 1, [FromUri] short startqueue = QueueType.FirstQueue, [FromUri] short endqueue = QueueType.LastQueue)
        {
            var items = await _qpo.DequeueAsync(count, startqueue, endqueue, CancellationToken.None);
            return Request.CreateResponse(HttpStatusCode.OK, items, new JsonMediaTypeFormatter());
        }

        /// <summary>
        /// Peeks at the items without removing them from the queue.
        /// </summary>
        /// <param name="startqueue">First queue to peek items from. Default to queue 0. Minimum value is 0, maximum value is the number of queues - 1.</param>
        /// <param name="endqueue">Last queue to peek items from. Default is -1, which indicates the maximum queue value.</param>
        /// <returns>HttpStatusCode.OK (200) with the number of QueuedItems&lt;MySample&gt; instances containing the returned items requested from one or more priorities from the queue or all of the remaining items if there are fewer than count items.
        /// HttpStatusCode.BadRequest (400) if the queue asked for is not within the valid range.
        /// HttpStatisCode.InternalServerError (500) an unknown error has occurred.</returns>
        /// <example>
        /// Examples:
        ///     Peek a single item from the highest priority queue containing an item       - /api/peek
        ///     Peek a single item from the priority 7 or lower queue containing an item    - /api/peek?startqueue=7
        ///     Peek a 10 items from the priority 2 or 3 queues if they contain an item     - /api/peek?startqueue=2&amp;endqueue=3
        /// </example>
        [HttpGet]
        [Route(@"peek", Name = "Peek")]
        public async Task<HttpResponseMessage> PeekItemsAsync([FromUri] short startqueue = QueueType.FirstQueue, [FromUri] short endqueue = QueueType.LastQueue)
        {
            var items = await _qpo.PeekItemAsync(startqueue, endqueue, CancellationToken.None);
            return Request.CreateResponse(HttpStatusCode.OK, items, new JsonMediaTypeFormatter());
        }

        /// <summary>
        /// Peeks at next set of item keys within a particular queue.
        /// </summary>
        /// <param name="queue">Queue to peek item keys from. Default to queue 0. Minimum value is 0, maximum value is the number of queues - 1.</param>
        /// <param name="top">Maximum number of items to return..</param>
        /// <param name="skip">Number of items to skip before starting to return items. This allow paging.</param>
        /// <returns>HttpStatusCode.OK (200) with the number of QueuedItems&lt;MySample&gt; instances containing the returned items requested from one or more priorities from the queue or all of the remaining items if there are fewer than count items.
        /// HttpStatusCode.BadRequest (400) if the queue asked for is not within the valid range.
        /// HttpStatisCode.InternalServerError (500) an unknown error has occurred.</returns>
        /// <example>
        /// Examples:
        ///     Peek keys from the default queue                            - /api/peekkeys
        ///     Peek keys priority 7 queue                                  - /api/peekkeys?queue=7
        ///     Peek 10 keys from the priority 2                            - /api/peekkeys?queue=2&amp;top=10
        ///     Peek 10 keys from the priority 2 starting at the 10th key   - /api/peekkeys?queue=2&amp;top=10&amp;skip=10
        /// </example>
        [HttpGet]
        [Route(@"peekkeys", Name = "PeekKeys")]
        public async Task<HttpResponseMessage> PeekItemKeysAsync([FromUri] short queue = QueueType.FirstQueue, [FromUri] int top = 1000, [FromUri] int skip = 0)
        {
            var keys = await _qpo.PeekKeysAsync(queue, top, skip, CancellationToken.None);
            return Request.CreateResponse(HttpStatusCode.OK, keys, new JsonMediaTypeFormatter());
        }

        /// <summary>
        /// Get items from this partition without removing them from the queue.
        /// </summary>
        /// <param name="top">Maximum number of items to return..</param>
        /// <param name="skip">Number of items to skip before starting to return items. This allow paging.</param>
        /// <returns>HttpStatusCode.OK (200) with the number of QueuedItems&lt;MySample&gt; instances containing the returned items requested from one or more priorities from the queue or all of the remaining items if there are fewer than count items.
        /// HttpStatusCode.BadRequest (400) if the queue asked for is not within the valid range.
        /// HttpStatisCode.InternalServerError (500) an unknown error has occurred.</returns>
        /// <example>
        /// Examples:
        ///     Get up to 1000 items from the queue.                            - /api/items
        ///     Get the first seven items from the queue.                       - /api/items?top=7
        ///     Gets the first 2 items from the queue after skipping 3 items    - /api/items?top=2&amp;skip=3
        /// </example>
        [HttpGet]
        [Route(@"items", Name = "Items")]
        public async Task<HttpResponseMessage> GetItemsAsync([FromUri] int top = 1000, [FromUri] int skip = 0)
        {
            var items = await _qpo.GetItemsAsync(top, skip, CancellationToken.None);
            return Request.CreateResponse(HttpStatusCode.OK, items, new JsonMediaTypeFormatter());
        }

        /// <summary>
        /// REST endpoint that enqueues items.
        /// </summary>
        /// <param name="queue">Queue to add the item into.</param>
        /// <param name="items">Array of MySampleItem instances to enqueue.</param>
        /// <param name="leaseSeconds">Number of seconds for the default lease duration for each item in the set of items. Defaults to the lease duration configured for the queue.</param>
        /// <param name="expirationMinutes">Number of minutes for the expiration for each item in the set of items. Defaults to the expiration duration configured for the queue.</param>
        /// <returns>HttpStatusCode.NoContent (200) if all items were successfully enqueued with a QueueItem&lt;string&gt; response item for each item in the body.
        /// HttpStatusCode.BadRequest (400) if no items were passed in the body.
        /// HttpStatusCode.BadRequest (417) if the queue, lease or expiration are not within their valid ranges.
        /// HttpStatisCode.InternalServerError (500) an unknown error has occurred.</returns>
        /// <example>
        /// Examples:
        ///     Enqueue the items passed in the body to the priority 0 queue                                                                    - /api/0
        ///     Enqueue the items passed in the body to the priority 7 queue                                                                    - /api/7
        ///     Enqueue the items passed in the body to the priority 0 queue setting the lease duration for all of these items to 45 seconds    - /api/0?leaseSeconds=45
        ///     Enqueue the items passed in the body to the priority 0 queue setting the expiration duration for all of these items to 2 hours  - /api/0?leaseSeconds=120
        /// </example>
        /// <remarks>This is not an idempotent operation.</remarks>
        [HttpPost]
        [Route(@"{queue}", Name = "Enqueue")]
        public async Task<HttpResponseMessage> EnqueueAsync([FromBody] IEnumerable<Item> items, [FromUri] short queue = QueueType.FirstQueue, [FromUri] int leaseSeconds = -1, [FromUri] int expirationMinutes = -1)
        {
            TimeSpan leaseDuration = (leaseSeconds > 0) ? TimeSpan.FromSeconds(leaseSeconds) : default(TimeSpan);
            TimeSpan expiration = (expirationMinutes > 0) ? TimeSpan.FromMinutes(expirationMinutes) : default(TimeSpan);

            try
            {
                // Enqueue the items.
                var results = await _qpo.EnqueueAsync(items, queue, leaseDuration, expiration, CancellationToken.None);
                return Request.CreateResponse(HttpStatusCode.OK, results);
            }
            catch(ArgumentNullException ex) { return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex); }
            catch(ArgumentOutOfRangeException ex) { return Request.CreateErrorResponse(HttpStatusCode.ExpectationFailed, ex); }
        }

        /// <summary>
        /// Extends the lease of a set of items as specified by their unique keys.
        /// </summary>
        /// <param name="keys">PopReceipt instances of the items to extend.</param>
        /// <param name="leaseSeconds">Duration of the new lease period in seconds.</param>
        /// <returns>
        ///     HttpStatusCode.OK (200) Successfully extended lease.
        ///     HttpStatusCode.NoContent (204) Call was made with an empty body -- no items were specified to have their lease extended.
        ///     HttpStatusCode.BadRequest (417) The lease range is not within a valid range.
        /// </returns>
        /// <example>
        /// Examples:
        ///     End the lease of the indicated items                              - /api/0
        ///     Extend the list of the indicated items by 30 seconds              - /api/30
        /// </example>
        /// <exception cref="ArgumentOutOfRangeException">Lease seconds is negative.</exception>
        [HttpPut]
        [Route(@"", Name = "ExtendLease")]
        public async Task<HttpResponseMessage> ExtendLeaseAsync([FromBody] PopReceipt[] keys, [FromUri] int leaseSeconds)
        {
            Guard.ArgumentInRange(leaseSeconds, 0, int.MaxValue, nameof(leaseSeconds));

            // If no ids were passed, then return an empty list.
            if (0 == keys.Length)
                return Request.CreateResponse(HttpStatusCode.NoContent, new bool[0]);

            TimeSpan leaseDuration = TimeSpan.FromSeconds(leaseSeconds);

            // Extend the leases and return an array of success indicators.
            IEnumerable<bool> results = await _qpo.ExtendLeaseAsync(keys, leaseDuration, CancellationToken.None);
            return Request.CreateResponse(HttpStatusCode.OK, results);
        }

        /// <summary>
        /// Deletes an item as specified by the key.
        /// </summary>
        /// <param name="key">PopReceipt instance of the item to delete.</param>
        /// <returns>HttpStatusCode.OK (200) with number of items for the specified queue.
        /// HttpStatusCode.NoContent (204) with an empty body -- no items were specified to be deleted.</returns>
        [HttpDelete]
        [Route(@"{key}", Name = "DeleteItem")]
        public async Task<HttpResponseMessage> DeleteItemAsync(PopReceipt key)
        {
            // Delete the items with the passed keys, returning the items.
            var result = await _qpo.DeleteItemAsync(key, CancellationToken.None);
            return Request.CreateResponse(HttpStatusCode.OK, result, new JsonMediaTypeFormatter());
        }
    }
}
