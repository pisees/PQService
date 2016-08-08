// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.ServiceFabric.Services.Remoting;

namespace QuickService.QueueClient
{
    /// <summary>
    /// Stateful queue service interface.
    /// </summary>
    /// <typeparam name="TItem">Type of the item contained by the queue.</typeparam>
    public interface IQueueService<TItem> : IService
        where TItem : IEquatable<TItem>
    {
        /// <summary>
        /// Gets the count from one of the collections.
        /// </summary>
        /// <param name="queues">Integer indicating the queue to return the count for. 
        /// If QueueClient.AllQueues (-1) is specified, then the count for all queues will be returned.
        /// If QueueClient.LeaseQueue (-2) is specified, then the leased item count will be returned.
        /// If QueueClient.ExpiredQueue (-3) is specified, then the expired collection count will be returned.
        /// If QueueClient.ItemQueue (-4) is specified, then the list of all items queued, leased or expired will be returned.</param>
        /// <param name="ct">CancellationToken. Defaults to CancellationToken.None</param>
        /// <returns>Long value containing the count.</returns>
        /// <remarks>The queue count does not included items that are leased or expired.</remarks>
        Task<long> CountAsync(QueueType queues, CancellationToken ct);

        /// <summary>
        /// Removed the top item from the queue.
        /// </summary>
        /// <param name="count">Number of items to retrieve.</param>
        /// <param name="startQueue">First queue to dequeue from. </param>
        /// <param name="endQueue">Last queue to dequeue from. -1 represents the last queue.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns>Array of QueuedItem instances.</returns>
        Task<IEnumerable<QueueItem<TItem>>> DequeueAsync(int count, QueueType startQueue, QueueType endQueue, CancellationToken cancellationToken);

        /// <summary>
        /// Enqueues a new item to the queue.
        /// </summary>
        /// <param name="items">Items to enqueue.</param>
        /// <param name="queue">Destination queue.</param>
        /// <param name="lease">Duration after a dequeue operation when the items lease will expire.</param>
        /// <param name="expiration">Duration after enqueue when the item expires and will not be processed.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        Task<IEnumerable<QueueItem<TItem>>> EnqueueAsync(IEnumerable<TItem> items, QueueType queue, TimeSpan lease, TimeSpan expiration, CancellationToken cancellationToken);

        /// <summary>
        /// Peeks at the item without removing it from the queue.
        /// </summary>
        /// <param name="startQueue">First queue to peek. </param>
        /// <param name="endQueue">Last queue to peek. -1 represents the last queue.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns>Returns the next QueueItem&lt;TItem&gt; item.</returns>
        Task<QueueItem<TItem>> PeekItemAsync(QueueType startQueue, QueueType endQueue, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the list of items in the queue.
        /// </summary>
        /// <param name="top">Integer value that sets the maximum number of items to be returned.</param>
        /// <param name="skip">Integer value that sets the number of items to skip before starting to return items.</param>
        /// <param name="token">CancellationToken instance.</param>
        /// <returns>List of QueueItem&lt;TItem&gt; items.</returns>
        Task<IEnumerable<QueueItem<TItem>>> GetItemsAsync(int top, int skip, CancellationToken token);

        /// <summary>
        /// Delete an item from the queue.
        /// </summary>
        /// <param name="key">PopReceipt key instance.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns>List of QueueItem&lt;TItem&gt; items.</returns>
        Task<QueueItem<TItem>> DeleteItemAsync(PopReceipt key, CancellationToken cancellationToken);

        /// <summary>
        /// Update the state of a set of leased items.
        /// </summary>
        /// <param name="keys">Array of item keys to update if present.</param>
        /// <param name="lease">New lease duration from now. If TimeSpan.Zero is passed, the lease will be released.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns>Boolean indicator of success or failure.</returns>
        Task<IEnumerable<bool>> ExtendLeaseAsync(IEnumerable<PopReceipt> keys, TimeSpan lease, CancellationToken cancellationToken);

        /// <summary>
        /// Releases the lease of a queue item and removes it from the list of leased items.
        /// </summary>
        /// <param name="keys">Keys of the items to release.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns>Boolean indicator of success or failure.</returns>
        Task<IEnumerable<bool>> ReleaseLeaseAsync(IEnumerable<PopReceipt> keys, CancellationToken cancellationToken);

    }
}
