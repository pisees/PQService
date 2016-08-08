// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuickService.QueueClient
{
    /// <summary>
    /// Item to place into the queue.
    /// </summary>
    [DataContract]
    public class Item : IEquatable<Item>
    {
        /// <summary>
        /// Name of the item
        /// </summary>
        [DataMember]
        public string Name;

        /// <summary>
        /// Date and time the item was added.
        /// </summary>
        [DataMember]
        public DateTimeOffset Added;

        /// <summary>
        /// Duration field.
        /// </summary>
        [DataMember]
        public TimeSpan Duration;

        /// <summary>
        /// Unique identifier.
        /// </summary>
        [DataMember]
        public Guid Id;

        /// <summary>
        /// Integer value.
        /// </summary>
        [DataMember]
        public int IntegerValue;

        /// <summary>
        /// Long integer value.
        /// </summary>
        [DataMember]
        public long LongIntegerValue;

        /// <summary>
        /// Byte array to vary the size of the item.
        /// </summary>
        [DataMember]
        public byte[] Bytes;

        /// <summary>
        /// IEquatable equals.
        /// </summary>
        /// <param name="other">Item to compare.</param>
        /// <returns>True if they are equal, otherwise false.</returns>
        public bool Equals(Item other)
        {
            if (this.Added != other.Added)
                return false;
            if (this.Bytes.Length != other.Bytes.Length)
                return false;
            if (this.Duration != other.Duration)
                return false;
            if (this.Id != other.Id)
                return false;
            if (this.IntegerValue != other.IntegerValue)
                return false;
            if (this.LongIntegerValue != other.LongIntegerValue)
                return false;
            if (this.Name != other.Name)
                return false;

            for(int i=0; i < this.Bytes.Length; i++)
            {
                if (this.Bytes[i] != other.Bytes[i])
                    return false;
            }

            return true;
        }
    }
}
