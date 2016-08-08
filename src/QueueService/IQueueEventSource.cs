// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.QueueService
{
    using Common.Diagnostics;
    using System;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// EventSource methods required
    /// </summary>
    public interface IQueueEventSource : IMinimalEventSource
    {
        /// <summary>
        /// Reports that an item has been added to a partition of the queue.
        /// </summary>
        /// <param name="transactionId">Transaction identifier.</param>
        /// <param name="partition">Partition identifier.</param>
        /// <param name="replica">Replica identifier.</param>
        /// <param name="id">Item key.</param>
        void QueueItemAdded(long transactionId, Guid partition, long replica, string id);

        /// <summary>
        /// Reports that an item has been removed from a partition of the queue.
        /// </summary>
        /// <param name="transactionId">Transaction identifier.</param>
        /// <param name="partition">Partition identifier.</param>
        /// <param name="replica">Replica identifier.</param>
        /// <param name="id">Item key.</param>
        /// <param name="duration">Duration the item spent in the queue in milliseconds.</param>
        void QueueItemLeased(long transactionId, Guid partition, long replica, string id, int duration);

        /// <summary>
        /// Reports that an item lease has completed for a partition of the queue.
        /// </summary>
        /// <param name="transactionId">Transaction identifier.</param>
        /// <param name="partition">Partition identifier.</param>
        /// <param name="replica">Replica identifier.</param>
        /// <param name="id">Item key.</param>
        /// <param name="succeeded">True if the operation succeeded, otherwise false.</param>
        /// <param name="duration">Duration of the lease in milliseconds.</param>
        void QueueItemLeaseComplete(long transactionId, Guid partition, long replica, string id, bool succeeded, int duration);

        /// <summary>
        /// Reports that an item has expired for a partition of the queue.
        /// </summary>
        /// <param name="transactionId">Transaction identifier.</param>
        /// <param name="partition">Partition identifier.</param>
        /// <param name="replica">Replica identifier.</param>
        /// <param name="id">Item key.</param>
        void QueueItemExpired(long transactionId, Guid partition, long replica, string id);

        /// <summary>
        /// Reports that an operation on an item has aborted.
        /// </summary>
        /// <param name="partition">Partition identifier.</param>
        /// <param name="replica">Replica identifier.</param>
        /// <param name="id">Item key.</param>
        /// <param name="msg">String containing the reason.</param>
        void QueueItemOperationAborted(Guid partition, long replica, string id, string msg);

        /// <summary>
        /// Reports that an operation on an item has aborted.
        /// </summary>
        /// <param name="partition">Partition identifier.</param>
        /// <param name="replica">Replica identifier.</param>
        /// <param name="id">Item key.</param>
        /// <param name="ex">Thrown exception.</param>
        void QueueItemOperationAborted(Guid partition, long replica, string id, Exception ex);

        /// <summary>
        /// Reports that a partition of the queue has exceeded a capacity warning or error.
        /// </summary>
        /// <param name="partition">Partition identifier.</param>
        /// <param name="replica">Replica identifier.</param>
        /// <param name="msg">Message indicates a warning or error.</param>
        /// <param name="count">Current number of items in the partition.</param>
        void QueueCapacity(Guid partition, long replica, string msg, int count);

        /// <summary>
        /// Reports that a partition of the queue has exceeded a capacity warning or error.
        /// </summary>
        /// <param name="partition">Partition identifier.</param>
        /// <param name="replica">Replica identifier.</param>
        /// <param name="ex">Thrown exception.</param>
        void QueueMethodException(Guid partition, long replica, Exception ex);

        /// <summary>
        /// Reports that a partition of the queue has exceeded a capacity warning or error.
        /// </summary>
        /// <param name="partition">Partition identifier.</param>
        /// <param name="replica">Replica identifier.</param>
        /// <param name="name">Name of the method.</param>
        /// <param name="msg">Optional message. Usually contains the method parameters.</param>
        void QueueMethodFailed(Guid partition, long replica, string name, string msg = "");

        /// <summary>
        /// Logs the start of a queue method.
        /// </summary>
        /// <param name="partition">Partition identifier.</param>
        /// <param name="replica">Replica identifier.</param>
        /// <param name="name">Name of the method.</param>
        /// <param name="msg">Optional message. Usually contains the method parameters.</param>
        void QueueMethodStart(Guid partition, long replica, string name, string msg = "");

        /// <summary>
        /// Reports that a partition of the queue has exceeded a capacity warning or error.
        /// </summary>
        /// <param name="method">Name of the method called. Will be filled in automatically if not specified.</param>
        /// <param name="partition">Partition identifier.</param>
        /// <param name="replica">Replica identifier.</param>
        /// <param name="duration">Duration of the call in milliseconds.</param>
        /// <param name="msg">Optional message.</param>
        void QueueMethodLogging(string method, Guid partition, long replica, long duration, string msg = "");

        /// <summary>
        /// Reports that a partition of the queue has exceeded a capacity warning or error.
        /// </summary>
        /// <param name="partition">Partition identifier.</param>
        /// <param name="state">Health state.</param>
        /// <param name="msg">Health message.</param>
        void QueueHealth(Guid partition, string state, string msg);

        /// <summary>
        /// Reports that the lease for an item was extended.
        /// </summary>
        /// <param name="duration">Duration of the new lease.</param>
        /// <param name="key">Item key.</param>
        /// <param name="partition">Partition identifier.</param>
        /// <param name="replica">Replica identifier.</param>
        void QueueItemLeaseExtended(double duration, string key, Guid partition, long replica);

        /// <summary>
        /// Reports that the an item was not found.
        /// </summary>
        /// <param name="key">Item key.</param>
        /// <param name="partition">Partition identifier.</param>
        /// <param name="replica">Replica identifier.</param>
        void QueueItemNotFound(string key, Guid partition, long replica);

        /// <summary>
        /// Reports that the an item was removed from the lease item queue.
        /// </summary>
        /// <param name="key">Item key.</param>
        /// <param name="partition">Partition identifier.</param>
        /// <param name="replica">Replica identifier.</param>
        void QueueItemRemoved(string key, Guid partition, long replica);

        /// <summary>
        /// Reports that an item was removed from a queue, but that item was not present in the list of items. This can occur
        /// if items are deleted, because they cannot be removed from the queue until a dequeue operation occurs.
        /// </summary>
        /// <param name="key">Item key.</param>
        /// <param name="partition">Partition identifier.</param>
        /// <param name="replica">Replica identifier.</param>
        void QueueItemNotPresentInItems(string key, Guid partition, long replica);

        /// <summary>
        /// Reports that an invalid lease item was found.
        /// </summary>
        /// <param name="key">Item key.</param>
        /// <param name="partition">Partition identifier.</param>
        /// <param name="replica">Replica identifier.</param>
        void QueueItemInvalidLease(string key, Guid partition, long replica);

        /// <summary>
        /// Reports that a queue transaction has been committed.
        /// </summary>
        /// <param name="transactionId">Transaction identifier.</param>
        /// <param name="partition">Partition identifier.</param>
        /// <param name="replica">Replica identifier.</param>
        /// <param name="commitSequenceNumber">Commit sequence number.</param>
        void QueueTransactionCommitted(long transactionId, Guid partition, long replica, long commitSequenceNumber);
    }
}
