// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("QueueServiceTests")]

namespace QuickService.QueueService
{
    using System.IO;
    using Microsoft.ServiceFabric.Data;
    using Common;
    using Newtonsoft.Json.Bson;
    using Newtonsoft.Json;
    using System;
    using QueueClient;

    /// <summary>
    /// Serializes QueueItem%lt;TItem&gt; instances.
    /// </summary>
    /// <typeparam name="TItem"></typeparam>
    public class QueueItemSerializer<TItem> : IStateSerializer<QueueItem<TItem>>
        where TItem : IEquatable<TItem>
    {
        /// <summary>
        /// Reads a MySampleItem instance from a binary stream.
        /// </summary>
        /// <param name="binaryReader">BinaryReader instance.</param>
        /// <returns>MySampleItem instance.</returns>
        public QueueItem<TItem> Read(BinaryReader binaryReader)
        {
            Guard.ArgumentNotNull(binaryReader, nameof(binaryReader));

            using (BsonReader reader = new BsonReader(binaryReader) { CloseInput = false })
            {
                JsonSerializer serializer = new JsonSerializer();
                QueueItem<TItem> item = serializer.Deserialize<QueueItem<TItem>>(reader);
                return item;
            }
        }

        /// <summary>
        /// Reads a MySampleItem instance from a binary reader.
        /// </summary>
        /// <param name="baseValue">MySampleItem base value.</param>
        /// <param name="binaryReader">BinaryReader instance.</param>
        /// <returns>MySampleItem instance.</returns>
        public QueueItem<TItem> Read(QueueItem<TItem> baseValue, BinaryReader binaryReader)
        {
            return Read(binaryReader);
        }

        /// <summary>
        /// Writes a MySampleItem instance to a binary stream.
        /// </summary>
        /// <param name="value">MySampleItem instance.</param>
        /// <param name="binaryWriter">BinaryWriter instance.</param>
        public void Write(QueueItem<TItem> value, BinaryWriter binaryWriter)
        {
            Guard.ArgumentNotNull(value, nameof(value));
            Guard.ArgumentNotNull(binaryWriter, nameof(binaryWriter));

            using (BsonWriter writer = new BsonWriter(binaryWriter) { CloseOutput = false })
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(writer, value);
            }
        }

        /// <summary>
        /// Writes a MySampleItem instance to a binary stream.
        /// </summary>
        /// <param name="baseValue">Base value instance.</param>
        /// <param name="targetValue">MySampleItem instance.</param>
        /// <param name="binaryWriter">BinaryWriter instance.</param>
        /// <remarks>baseValue is provided to enable delta serialization in future releases.</remarks>
        public void Write(QueueItem<TItem> baseValue, QueueItem<TItem> targetValue, BinaryWriter binaryWriter)
        {
            Write(targetValue, binaryWriter);
        }
    }
}
