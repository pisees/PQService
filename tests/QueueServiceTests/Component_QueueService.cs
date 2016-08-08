// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QueueServiceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Threading;
    using QuickService.Common.Queue;
    using System;
    using System.Collections.Generic;
    using QuickService.QueueClient;
    using QuickService.Common;
    using System.Threading.Tasks;

    /// <summary>
    /// Unit test to validate that the JSON configuration file can be parsed into the destination type. Can't use the entire
    /// framework because ConfigurationPackage, ConfigSection and Package cannot be mocked.
    /// </summary>
    /// <remarks>This must be run on a box that has Service Fabric installed and running the application.</remarks>
    [TestClass]
    public class QueueService_ComponentTests
    {
        const string c_ListenerName = "OwinListener";
        const string serviceUri = "fabric:/PriorityQueueSample/PriorityQueueService";

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {

        }

        /// <summary>
        /// Creates a random instance of type Item.
        /// </summary>
        /// <returns></returns>
        private static Item CreateRandomInstance()
        {
            Item item = new Item()
            {
                Added = DateTimeOffset.Now,
                Duration = TimeSpan.FromSeconds(RandomThreadSafe.Instance.NextDouble()),
                Id = Guid.NewGuid(),
                IntegerValue = RandomThreadSafe.Instance.Next(),
                LongIntegerValue = RandomThreadSafe.Instance.Next(),
                Name = $"ItemValue_{RandomThreadSafe.Instance.Next()}",
                Bytes = new byte[256]
            };

            RandomThreadSafe.Instance.NextBytes(item.Bytes);
            return item;
        }

        /// <summary>
        /// Test various queue operations. Queue must be empty for this test to pass.
        /// </summary>
        [TestMethod]
        public async Task Component_Test()
        {
            Item i1 = CreateRandomInstance();
            Item i2 = CreateRandomInstance();
            Item i3 = CreateRandomInstance();

            HttpQueueClient<Item> qc = new HttpQueueClient<Item>(new Uri(serviceUri), c_ListenerName);
            Assert.IsNotNull(qc);

            long allBase = await qc.CountAsync(QueueType.AllQueues).ConfigureAwait(false);
            long leaseBase = await qc.CountAsync(QueueType.LeaseQueue).ConfigureAwait(false);
            long expiredBase = await qc.CountAsync(QueueType.ExpiredQueue).ConfigureAwait(false);
            long itemsBase = await qc.CountAsync(QueueType.ItemQueue).ConfigureAwait(false);

            // Add an single item to the queue and validate the count has changed.
            await qc.EnqueueAsync(new[] { i1 }, QueueType.FirstQueue, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5), CancellationToken.None).ConfigureAwait(false);
            long count = await qc.CountAsync(QueueType.AllQueues).ConfigureAwait(false);
            Assert.AreEqual(1 + allBase, count);
            count = await qc.CountAsync(QueueType.ItemQueue).ConfigureAwait(false);
            Assert.AreEqual(1 + itemsBase, count);

            // Add a batch of items to the queue and validate the count has changed.
            await qc.EnqueueAsync(new[] { i1, i2, i3 }, QueueType.FirstQueue, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5), CancellationToken.None).ConfigureAwait(false);
            count = await qc.CountAsync(QueueType.AllQueues, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(4 + allBase, count);
            count = await qc.CountAsync(QueueType.ItemQueue, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(4 + itemsBase, count);

            // Enqueue a single item to the second queue and check the counts. 
            await qc.EnqueueAsync(new[] { i1 }, 1, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(5), CancellationToken.None).ConfigureAwait(false);
            count = await qc.CountAsync(QueueType.AllQueues).ConfigureAwait(false);
            Assert.AreEqual(5 + allBase, count);
            count = await qc.CountAsync(QueueType.ItemQueue, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(5 + itemsBase, count);

            // Dequeue a single item from the first queue and check the counts.
            IEnumerable<QueueItem<Item>> items = await qc.DequeueAsync(1, QueueType.FirstQueue, QueueType.LastQueue, CancellationToken.None).ConfigureAwait(false);
            count = await qc.CountAsync(QueueType.AllQueues, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(4 + allBase, count);
            count = await qc.CountAsync(QueueType.LeaseQueue, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(1 + leaseBase, count);
            count = await qc.CountAsync(QueueType.ItemQueue, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(5 + itemsBase, count);

            // Peek the next item.
            QueueItem<Item> item = await qc.PeekItemAsync(QueueType.FirstQueue, QueueType.LastQueue, CancellationToken.None).ConfigureAwait(false);
            Assert.IsNotNull(item);
            Console.WriteLine(item.ToString());

            // Get a list of the items.
            items = await qc.GetItemsAsync(0, 1000, 0).ConfigureAwait(false);
            foreach (var qi in items)
                Console.WriteLine(qi.Item.Name);

            // Wait for the lease to expire and check the counts.
            Thread.Sleep(TimeSpan.FromSeconds(125));
            count = await qc.CountAsync(QueueType.AllQueues, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(4 + allBase, count);
            count = await qc.CountAsync(QueueType.LeaseQueue, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(0 + leaseBase, count);
            count = await qc.CountAsync(QueueType.ItemQueue, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(5 + itemsBase, count);

            // Dequeue again, release the lease and check the counts.
            items = await qc.DequeueAsync(1, QueueType.FirstQueue, QueueType.LastQueue, CancellationToken.None).ConfigureAwait(false);
            await ReleaseLeasesAsync(qc, items);
            count = await qc.CountAsync(QueueType.AllQueues, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(4 + allBase, count);
            count = await qc.CountAsync(QueueType.LeaseQueue, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(0 + leaseBase, count);
            count = await qc.CountAsync(QueueType.ItemQueue, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(4 + itemsBase, count);

            // Dequeue two, release the lease and check the counts.
            items = await qc.DequeueAsync(2, QueueType.FirstQueue, QueueType.LastQueue, CancellationToken.None).ConfigureAwait(false);
            await ReleaseLeasesAsync(qc, items);
            count = await qc.CountAsync(QueueType.AllQueues, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(2 + allBase, count);
            count = await qc.CountAsync(QueueType.LeaseQueue, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(0 + leaseBase, count);
            count = await qc.CountAsync(QueueType.ItemQueue, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(2 + itemsBase, count);

            // Dequeue two, release the lease and check the counts.
            items = await qc.DequeueAsync(2, QueueType.FirstQueue, QueueType.LastQueue, CancellationToken.None).ConfigureAwait(false);
            await ReleaseLeasesAsync(qc, items);
            count = await qc.CountAsync(QueueType.AllQueues, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(0 + allBase, count);
            count = await qc.CountAsync(QueueType.LeaseQueue, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(0 + leaseBase, count);
            count = await qc.CountAsync(QueueType.ItemQueue, CancellationToken.None).ConfigureAwait(false);
            Assert.AreEqual(0 + itemsBase, count);

        }

        /// <summary>
        /// Builds an array of PopReceipt instances from items.
        /// </summary>
        /// <param name="items">List of items to build they key array from.</param>
        /// <returns>Array of PopReceit keys.</returns>
        private IList<PopReceipt> KeyArrayFromItems(IEnumerable<QueueItem<Item>> items)
        {
            // Allocate the array.
            List<PopReceipt> keys = new List<PopReceipt>();

            // Populate the key array.
            foreach(QueueItem<Item> item in items)
            {
                keys.Add(item.Key);
            }

            return keys;
        }

        /// <summary>
        /// Deletes all of the items in the list.
        /// </summary>
        /// <param name="client">QueueService client.</param>
        /// <param name="items">List of QueueItem&lt;string&gt; instances.</param>
        private async Task DeleteItemsAsync(IQueueService<Item> client, IList<QueueItem<Item>> items)
        {
            // Release the lease.
            var results = await client.ReleaseLeaseAsync(KeyArrayFromItems(items), CancellationToken.None).ConfigureAwait(false);
            foreach (bool b in results)
            {
                Assert.IsTrue(b);
            }
        }

        /// <summary>
        /// Releases the lease for each of the items in the list.
        /// </summary>
        /// <param name="client">QueueClient instance.</param>
        /// <param name="items">Items to release leases.</param>
        private async Task ReleaseLeasesAsync(HttpQueueClient<Item> client, IEnumerable<QueueItem<Item>> items)
        {
            // Release the lease.
            var results = await client.ReleaseLeaseAsync(KeyArrayFromItems(items), CancellationToken.None).ConfigureAwait(false);
            foreach(bool b in results)
            {
                Assert.IsTrue(b);
            }
        }
    }
}
