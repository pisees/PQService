// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace QuickService.PriorityQueueService
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Test class used to ensure the queue can handle generic strings and specific types.
    /// </summary>
    [DataContract]
    public sealed class MySampleItem : IEquatable<MySampleItem>
    {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        [DataMember]
        public int Value { get; set; }

        /// <summary>
        /// Indicates if this is equal to the passed item.
        /// </summary>
        /// <param name="other">Item to compare.</param>
        /// <returns>True if they are equal, otherwise false.</returns>
        public bool Equals(MySampleItem other)
        {
            if (null == other)
                return false;

            return (this.Value == other.Value);
        }
    }
}
