// ------------------------------------------------------------
//  <copyright file="Component_QueueClient.cs" Company="Microsoft Corporation">
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// </copyright>
// ------------------------------------------------------------

namespace QueueServiceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Threading;
    using System.Threading.Tasks;
    using QuickService.Common;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using QuickService.QueueClient;
    using System.Fabric;

    /// <summary>
    /// QueueClient tests require a running application and fabric cluster.
    /// </summary>
    /// <remarks>This must be run on a box that has Service Fabric installed and running the application.</remarks>
    [TestClass]
    public class QueueClient_ComponentTests
    {
        const int numThreads = 10;
        int threadStartCount;
        int threadCompleteCount;

        const string c_ListenerName = "OwinListener";
        const string serviceUri = "fabric:/PriorityQueueSample/PriorityQueueService";

        Item i1 = CreateRandomInstance();
        Item i2 = CreateRandomInstance();
        Item i3 = CreateRandomInstance();

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

        [TestMethod]
        public void Constructor_Test()
        {
            HttpQueueClient<string> qc = new HttpQueueClient<string>(new Uri(serviceUri), c_ListenerName);
            Assert.IsNotNull(qc);

            threadStartCount = 0;
            threadCompleteCount = 0;

            // Check it with multiple threads doing the initialization.
            Thread[] threads = new Thread[numThreads];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(ThreadProc);
                threads[i].Start(i);
            }

            Thread.Sleep(5000);
            Assert.AreEqual(threadStartCount, threadCompleteCount);
        }

        [TestMethod]
        public async Task GetPriorityCount_Test()
        {
            HttpQueueClient<string> qc = new HttpQueueClient<string>(new Uri(serviceUri), c_ListenerName);
            Assert.IsNotNull(qc);

            int count = await qc.PriorityCountAsync().ConfigureAwait(false);
            Assert.AreEqual(5, count);
        }

        [TestMethod]
        public async Task GetCount_Test()
        {
            long queueLength = -1;

            // Get the queue length for the cluster using the load APIs.
            FabricClient sfc = new FabricClient();
            var li = await sfc.QueryManager.GetClusterLoadInformationAsync();
            foreach(var lmi in li.LoadMetricInformationList)
            {
                if ("QueueLength" == lmi.Name)
                {
                    queueLength = lmi.ClusterLoad;
                    break;
                }
            }

            Assert.IsTrue(queueLength >= 0);

            // Get the cluster count based on the count of each queue partition.
            HttpQueueClient<string> qc = new HttpQueueClient<string>(new Uri(serviceUri), c_ListenerName);
            Assert.IsNotNull(qc);

            long count = await qc.CountAsync().ConfigureAwait(false);
            Assert.AreEqual(queueLength, count);
        }

        [TestMethod]
        public async Task Enqueue_Test()
        {
            HttpQueueClient<Item> qc = new HttpQueueClient<Item>(new Uri(serviceUri), c_ListenerName);
            Assert.IsNotNull(qc);

            Item[] items = new Item[] { i1, i2, i3 };

            // Clear the existing queue.
            await ClearQueueAsync();

            // Get the queue count at the beginning.
            long beginCount = await qc.CountAsync().ConfigureAwait(false);

            // Enqueue a single item.
            var response = await qc.EnqueueAsync(items).ConfigureAwait(false);
            Assert.IsNotNull(response);

            List<QueueItem<Item>> responseItems = new List<QueueItem<Item>>(response);
            Assert.AreEqual(items.Length, responseItems.Count);
            Assert.AreEqual(0, responseItems[0].Queue);
            Assert.AreEqual(0, responseItems[0].DequeueCount);
            Assert.AreEqual(TimeSpan.FromMinutes(10), responseItems[0].LeaseDuration);
            Assert.AreEqual(DateTimeOffset.MaxValue, responseItems[0].ExpirationTime);

            for(int i=0; i < items.Length; i++)
            {
                Assert.IsTrue(items[i].Equals(responseItems[i].Item));
            }

            // Get the queue count at the end.
            long endCount = await qc.CountAsync();
            Assert.AreEqual(beginCount + items.Length, endCount);
        }

        [TestMethod]
        public async Task Dequeue_Test()
        {
            HttpQueueClient<Item> qc = new HttpQueueClient<Item>(new Uri(serviceUri), c_ListenerName);
            Assert.IsNotNull(qc);

            Item[] items = new Item[] { i1, i2, i3 };

            // Clear the existing queue.
            await ClearQueueAsync();

            // Check that the count is zero.
            long count = await qc.CountAsync().ConfigureAwait(false);
            Assert.AreEqual(0, count);

            // Enqueue the items.
            var response = await qc.EnqueueAsync(items).ConfigureAwait(false);
            Assert.IsNotNull(response);

            // Check that all items were queued.
            count = await qc.CountAsync().ConfigureAwait(false);
            Assert.AreEqual(items.Length, count);

            // Dequeue items.
            response = await qc.DequeueAsync().ConfigureAwait(false);
            Assert.IsNotNull(response);
            QueueItem<Item> item = response.First();
            Assert.IsNotNull(item);
            Assert.IsTrue(i1.Equals(item.Item));
            Assert.AreEqual(0, item.Queue);
            Assert.AreEqual(1, item.DequeueCount);
            Assert.AreEqual(TimeSpan.FromMinutes(10), item.LeaseDuration);
            Assert.AreEqual(DateTimeOffset.MaxValue, item.ExpirationTime);

            // Get the queue count at the end. The queue must be started empty for this test to pass correctly.
            Assert.AreEqual(3, await qc.CountAsync(QueueType.ItemQueue).ConfigureAwait(false));
            Assert.AreEqual(1, await qc.CountAsync(QueueType.LeaseQueue).ConfigureAwait(false));
            Assert.AreEqual(2, await qc.CountAsync(QueueType.AllQueues).ConfigureAwait(false));
        }

        [TestMethod]
        public async Task DeleteItems_Test()
        {
            HttpQueueClient<Item> qc = new HttpQueueClient<Item>(new Uri(serviceUri), c_ListenerName);
            Assert.IsNotNull(qc);

            // Check that the count is zero.
            long beginQueueCount = await qc.CountAsync().ConfigureAwait(false);
            long beginItemCount = await qc.CountAsync(QueueType.ItemQueue).ConfigureAwait(false);

            // Add three items to the queue.
            Item[] items = new Item[] { i1, i2, i3 };
            var enqueueResult = await qc.EnqueueAsync(items).ConfigureAwait(false);
            Assert.IsNotNull(enqueueResult);

            foreach(QueueItem<Item> item in enqueueResult)
            {
                var deletedItem = await qc.DeleteItemAsync(item.Key, CancellationToken.None).ConfigureAwait(false);

                Assert.AreEqual(item.Key, deletedItem.Key);
                Assert.AreEqual(item.DequeueCount, deletedItem.DequeueCount);
                Assert.AreEqual(item.EnqueueTime, deletedItem.EnqueueTime);
                Assert.AreEqual(item.ExpirationTime, deletedItem.ExpirationTime);
                Assert.IsTrue(item.Item.Equals(deletedItem.Item));
                Assert.AreEqual(item.LeasedUntil, deletedItem.LeasedUntil);
                Assert.AreEqual(item.LeaseDuration, deletedItem.LeaseDuration);
                Assert.AreEqual(item.Queue, deletedItem.Queue);
            }

            Assert.AreEqual(beginQueueCount + items.Count(), await qc.CountAsync().ConfigureAwait(false));
            Assert.AreEqual(beginItemCount, await qc.CountAsync(QueueType.ItemQueue).ConfigureAwait(false));
        }

        /// <summary>
        /// ThreadProc for testing multiple thread construction.
        /// </summary>
        /// <param name="data">Thread offset count.</param>
        private void ThreadProc(object data)
        {
            int offset = (int)data;
            Interlocked.Increment(ref threadStartCount);

            HttpQueueClient<Item> qc = new HttpQueueClient<Item>(new Uri(serviceUri), c_ListenerName);
            if (null != qc)
            {
                Console.WriteLine($"Thread {offset}[{Thread.CurrentThread.ManagedThreadId}] completed.");
            }

            Interlocked.Increment(ref threadCompleteCount);
        }

        [TestMethod]
        public async Task ClearQueue_Test()
        {
            HttpQueueClient<Item> qc = new HttpQueueClient<Item>(new Uri(serviceUri), c_ListenerName);

            long queueCount = await qc.CountAsync().ConfigureAwait(false);
            long itemCount = await qc.CountAsync(QueueType.ItemQueue).ConfigureAwait(false);

            // If there are no items, add some so there is something to clear.
            if (0 == (queueCount + itemCount))
            {
                Item[] items = new Item[] { i1, i2, i3 };
                var response = await qc.EnqueueAsync(items).ConfigureAwait(false);
                Assert.IsNotNull(response);
                queueCount = items.Length;
            }

            await ClearQueueAsync();

            Assert.AreEqual(0, await qc.CountAsync().ConfigureAwait(false));
            Assert.AreEqual(0, await qc.CountAsync(QueueType.ItemQueue).ConfigureAwait(false));
        }

        /// <summary>
        /// Clears all of the items from a queue partition.
        /// </summary>
        private async Task ClearQueueAsync()
        {
            HttpQueueClient<Item> qc = new HttpQueueClient<Item>(new Uri(serviceUri), c_ListenerName);
            Assert.IsNotNull(qc);

            long queueCount = await qc.CountAsync().ConfigureAwait(false);
            long itemCount = await qc.CountAsync(QueueType.ItemQueue);

            // Visit each partition looking for items.
            for(long partition = 0; partition < qc.ServicePartitionCount; partition++)
            {

                // Get the items in this partition.
                IEnumerable<QueueItem<Item>> results = await qc.GetItemsAsync(partition).ConfigureAwait(false);
                foreach (QueueItem<Item> qi in results)
                {
                    var deleteResults = await qc.DeleteItemAsync(qi.Key, CancellationToken.None).ConfigureAwait(false);
                }
            }

            int attempt = qc.ServicePartitionCount;

            // Get the count, there may be items hanging out in the queues, which won't be removed until a dequeue operation for that partition is attempted
            // and the corresponding item is not found in the list of items.
            while((await qc.CountAsync().ConfigureAwait(false) > 0) && (attempt-- > 0))
            {
                IEnumerable<QueueItem<Item>> results = await qc.DequeueAsync().ConfigureAwait(false);
            }
        }
    }
}
