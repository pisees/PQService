// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QueueServiceTests
{
    using System;
    using System.IO;
    using QuickService.Common.Queue;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Text;
    using QuickService.QueueService;
    using QuickService.QueueClient;

    /// <summary>
    /// Unit test to validate that the JSON config file can be parsed into the destination type. Can't use the entire
    /// framework because ConfigurationPackage, ConfigSection and Package cannot be mocked.
    /// </summary>
    /// <remarks>This must be run on a box that has Service Fabric installed and running the application.</remarks>
    [TestClass]
    public class QueueItemSerializer_UnitTests
    {
        static TestClass tc1 = null;
        static Guid id1 = Guid.NewGuid();
        static DateTimeOffset now1 = DateTimeOffset.Now;

        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            tc1 = new TestClass() { d_value = 1.1, i_value = 11, s_value = "mystringvalue" };
        }

        [TestMethod]
        public void ConstructorTests()
        {
            QueueItemSerializer<TestClass> qis = new QueueItemSerializer<TestClass>();
            Assert.IsNotNull(qis);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        [TestMethod]
        public void TestClass_SerializeDeserialize_Test()
        {
            PopReceipt key = PopReceipt.NewPopReceipt(0);
            QueueItem<TestClass> qitc1 = new QueueItem<TestClass>(key, 0, tc1, TimeSpan.FromSeconds(30), DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(30)), DateTimeOffset.UtcNow.Add(TimeSpan.FromMinutes(20)), DateTimeOffset.UtcNow, 0);
            QueueItem<TestClass> qitc2;

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms, Encoding.Default, true))
                {
                    QueueItemSerializer<TestClass> serializer = new QueueItemSerializer<TestClass>();
                    serializer.Write(qitc1, writer);
                }

                ms.Seek(0, SeekOrigin.Begin);

                using (BinaryReader reader = new BinaryReader(ms, Encoding.Default, true))
                {
                    QueueItemSerializer<TestClass> serializer = new QueueItemSerializer<TestClass>();
                    qitc2 = serializer.Read(reader);
                }
            }

            Assert.IsNotNull(qitc2);
            Assert.AreEqual(qitc1.Key, qitc2.Key);
            Assert.AreEqual(qitc1.Queue, qitc2.Queue);
            Assert.AreEqual(qitc1.DequeueCount, qitc2.DequeueCount);
            Assert.IsTrue(qitc1.EnqueueTime.Subtract(qitc2.EnqueueTime).TotalSeconds < 1, "EnqueueTime is off by more than 1 second.");
            Assert.IsTrue(qitc1.ExpirationTime.Subtract(qitc2.ExpirationTime).TotalSeconds < 1, "ExpirationTime is off by more than 1 second.");
            Assert.IsTrue(qitc1.LeasedUntil.Subtract(qitc2.LeasedUntil).TotalSeconds < 1, "LeasedUntil is off by more than 1 second.");
            Assert.AreEqual(qitc1.LeaseDuration, qitc2.LeaseDuration);
            Assert.AreEqual(qitc1.Item.d_value, qitc2.Item.d_value);
            Assert.AreEqual(qitc1.Item.i_value, qitc2.Item.i_value);
            Assert.AreEqual(qitc1.Item.s_value, qitc2.Item.s_value);
        }
    }
}
