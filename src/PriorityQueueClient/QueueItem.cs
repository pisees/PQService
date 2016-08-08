// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("QueueServiceTests")]

namespace QuickService.QueueClient
{
    using System;
    using System.Runtime.Serialization;
    
    /// <summary>
    /// Item held within the queue.
    /// </summary>
    [DataContract]
    public struct QueueItem<TItem> : IEquatable<QueueItem<TItem>>
        where TItem : IEquatable<TItem>
    {
        /// <summary>
        /// Gets the QueueItem&lt;TItem&gt; representing the default value.
        /// </summary>
        public static readonly QueueItem<TItem> Default = new QueueItem<TItem>();

        #region Properties 

        /// <summary>
        /// Gets or sets the PopReceipt containing the key used to identify the queued item.
        /// </summary>
        [DataMember]
        public PopReceipt Key { get; private set; }

        /// <summary>
        /// Gets or sets the date and time the lease for this item expires. 
        /// It is calculated during dequeue using the UTC time plus the LeaseDuration below.
        /// </summary>
        [DataMember]
        public DateTimeOffset LeasedUntil { get; private set; }

        /// <summary>
        /// Gets or sets the lease duration for this item. 
        /// </summary>
        [DataMember]
        public TimeSpan LeaseDuration { get; private set; }

        /// <summary>
        /// Gets or sets the date and time in UTC of when the item was enqueued into the queue.
        /// </summary>
        [DataMember]
        public DateTimeOffset EnqueueTime { get; private set; }

        /// <summary>
        /// Gets or sets the date and time in UTC when this message expires and will be removed from any queue.
        /// </summary>
        [DataMember]
        public DateTimeOffset ExpirationTime { get; private set; }

        /// <summary>
        /// Gets or sets the priority of the item.
        /// </summary>
        [DataMember]
        public int Queue { get; private set; }

        /// <summary>
        /// Gets or sets the number of times the item has been dequeued.
        /// </summary>
        [DataMember]
        public int DequeueCount { get; private set; }

        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        [DataMember]
        public TItem Item { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// QueueItem constructor creating an leased or un-leased QueueItem with the passed expiration and enqueue time.
        /// </summary>
        /// <param name="key">PopReceipt containing the unique key for the item while in the queue.</param>
        /// <param name="queue">Destination queue of the item.</param>
        /// <param name="item">Item instance.</param>
        /// <param name="lease">Duration of the lease.</param>
        /// <param name="leaseUntil">Date and time the lease will expire.</param>
        /// <param name="expiresAt">Date and time the item expires.</param>
        /// <param name="enqueueTime">Date and time this item was first enqueued.</param>
        /// <param name="count">Number of times this item has been previously dequeued.</param>
        public QueueItem(PopReceipt key, int queue, TItem item, TimeSpan lease, DateTimeOffset leaseUntil, DateTimeOffset expiresAt, DateTimeOffset enqueueTime, int count)
        {
            Key = key;
            Queue = queue;
            Item = item;
            EnqueueTime = enqueueTime;
            LeasedUntil = leaseUntil;
            LeaseDuration = lease;
            ExpirationTime = expiresAt;
            DequeueCount = count;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Compares two object instances for equality.
        /// </summary>
        /// <param name="other">Object instance to compare.</param>
        /// <returns>True if they are equal, otherwise false.</returns>
        public override bool Equals(object other)
        {
            if (null == other)
                return false;

            if (other.GetType() != this.GetType())
                return false;

            return Equals((QueueItem<TItem>)other);
        }

        /// <summary>
        /// Compares two QueueItem&lt;TItem&gt; instances.
        /// </summary>
        /// <param name="other">QueueItem&lt;TItem&gt; instance to compare.</param>
        /// <returns>True if they are equal, otherwise false.</returns>
        public bool Equals(QueueItem<TItem> other)
        {
            if (Key != other.Key)
                return false;
            if (Queue != other.Queue)
                return false;
            if (EnqueueTime != other.EnqueueTime)
                return false;
            if (LeasedUntil != other.LeasedUntil)
                return false;
            if (LeaseDuration != other.LeaseDuration)
                return false;
            if (ExpirationTime != other.ExpirationTime)
                return false;
            if (DequeueCount != other.DequeueCount)
                return false;

            if ((null == Item) && (null == other.Item))
                return true;
            else if ((null == Item) || (null == other.Item))
                return false;
            else
                return Item.Equals(other.Item);
        }

        /// <summary>
        /// Returns the hash code for this object.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return Key.GetHashCode() ^ Queue.GetHashCode() ^ EnqueueTime.GetHashCode() ^ LeasedUntil.GetHashCode() ^ LeaseDuration.GetHashCode() ^ ExpirationTime.GetHashCode() ^ DequeueCount.GetHashCode() ^ Item.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"{Key}\n\tLeasedUntil: {LeasedUntil}\n\tLeaseDuration: {LeaseDuration}\n\tEnqueueTime: {EnqueueTime}\n\tExpirationTime: {ExpirationTime}\n\tQueue: {Queue}\n\tDequeueCount: {DequeueCount}\n\tItem: {Item}";
        }

        /// <summary>
        /// Not equals operator.
        /// </summary>
        /// <returns>False if equal, otherwise True.</returns>
        public static bool operator !=(QueueItem<TItem> qi1, QueueItem<TItem> qi2)
        {
            return !(qi1.Equals(qi2));
        }

        /// <summary>
        /// Equals operator.
        /// </summary>
        /// <returns>True if equal, otherwise False.</returns>
        public static bool operator ==(QueueItem<TItem> qi1, QueueItem<TItem> qi2)
        {
            return (qi1.Equals(qi2));
        }

        /// <summary>
        /// Creates a new QueueItem instance with an updated LeaseUntil property.
        /// </summary>
        /// <param name="leaseUntil">DateTimeOffset value of LeaseUntil property.</param>
        /// <returns>QueueItem&lt;TItem&gt; instance.</returns>
        public QueueItem<TItem> UpdateWith(DateTimeOffset leaseUntil)
        {
            return new QueueItem<TItem>(Key, Queue, Item, LeaseDuration, leaseUntil, ExpirationTime, EnqueueTime, DequeueCount);
        }

        /// <summary>
        /// Creates a new QueueItem instance with an updated LeaseDuration and LeaseUntil property values.
        /// </summary>
        /// <param name="leaseDuration">TimeSpan value of LeaseDuration property.</param>
        /// <param name="leaseUntil">DateTimeOffset value of LeaseUntil property.</param>
        /// <returns>QueueItem&lt;TItem&gt; instance.</returns>
        public QueueItem<TItem> UpdateWith(TimeSpan leaseDuration, DateTimeOffset leaseUntil)
        {
            return new QueueItem<TItem>(Key, Queue, Item, leaseDuration, leaseUntil, ExpirationTime, EnqueueTime, DequeueCount);
        }

        /// <summary>
        /// Creates a new QueueItem instance with an updated Queue and LeaseUntil property values.
        /// </summary>
        /// <param name="queue">Integer value of Queue property.</param>
        /// <param name="leaseUntil">DateTimeOffset value of LeaseUntil property.</param>
        /// <returns>QueueItem&lt;TItem&gt; instance.</returns>
        public QueueItem<TItem> UpdateWith(int queue, DateTimeOffset leaseUntil)
        {
            return new QueueItem<TItem>(Key, queue, Item, LeaseDuration, leaseUntil, ExpirationTime, EnqueueTime, DequeueCount);
        }

        #endregion
    }
}
