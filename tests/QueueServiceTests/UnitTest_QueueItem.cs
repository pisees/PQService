// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QueueServiceTests
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using QuickService.Common.Queue;
    using Newtonsoft.Json;
    using System.IO;
    using Newtonsoft.Json.Bson;
    using System.Text;
    using QuickService.QueueClient;
    using QuickService.QueueService;

    [TestClass]
    public class QueueItem_UnitTest
    {
        static TestClass tc1 = new TestClass() { i_value = 1, d_value = 1.1, s_value = "one" };
        static TestClass tc2 = new TestClass() { i_value = 2, d_value = 2.2, s_value = "two" };

        [TestMethod]
        public void QueueItemValidateInterfaces()
        {
            // Create an empty QueueItem and compare it to the default.
            QueueItem<TestClass> qiDefault = default(QueueItem<TestClass>);
            Assert.IsTrue(QueueItem<TestClass>.Default.Equals(qiDefault));

            // Create an initialized item.
            PopReceipt key = PopReceipt.NewPopReceipt(0);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            TimeSpan lease = TimeSpan.FromMinutes(2);
            DateTimeOffset leaseUntil = now.Add(lease);
            TimeSpan expiration = TimeSpan.FromMinutes(10);
            DateTimeOffset expiresAt = now.Add(expiration);

            // Validate the item can be cast to the supported interfaces.
            QueueItem<TestClass> qi1 = new QueueItem<TestClass>(key, 3, tc2, lease, leaseUntil, expiresAt, now, 2);
            IEquatable<QueueItem<TestClass>> ie = (IEquatable<QueueItem<TestClass>>) qi1;
            Assert.IsNotNull(ie);

            // and use the interfaces.
            Assert.IsFalse(ie.Equals(qiDefault));
        }

        [TestMethod]
        public void QueueItemConstructorTest()
        {
            PopReceipt key = PopReceipt.NewPopReceipt(0);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            TimeSpan lease = TimeSpan.FromMinutes(2);
            DateTimeOffset leaseUntil = now.Add(lease);
            TimeSpan expiration = TimeSpan.FromMinutes(10);
            DateTimeOffset expiresAt = now.Add(expiration);

            // Priority, lease duration, lease date/time, expiration date/time, enqueue date/time, dequeue count and item.
            QueueItem<TestClass> qi3 = new QueueItem<TestClass>(key, 3, tc2, lease, leaseUntil, expiresAt, now, 2);
            Assert.IsNotNull(qi3);
            Assert.AreEqual(key, qi3.Key);
            Assert.AreEqual(3, qi3.Queue);
            Assert.IsTrue(leaseUntil.Subtract(qi3.LeasedUntil).TotalSeconds < 1);
            Assert.AreEqual(lease, qi3.LeaseDuration);
            Assert.IsTrue(DateTimeOffset.UtcNow.Subtract(qi3.EnqueueTime).TotalSeconds < 1);
            Assert.IsTrue(DateTimeOffset.UtcNow.Add(expiration).Subtract(qi3.ExpirationTime).TotalSeconds < 1);
            Assert.AreEqual<int>(2, qi3.DequeueCount);
            Assert.AreEqual(tc2, qi3.Item);
        }

        [TestMethod]
        public void QueueItemEquals_Test()
        {
            PopReceipt key = PopReceipt.NewPopReceipt(0);
            DateTimeOffset now = DateTimeOffset.UtcNow;
            TimeSpan lease = TimeSpan.FromMinutes(2);
            DateTimeOffset leaseUntil = now.Add(lease);
            TimeSpan expiration = TimeSpan.FromMinutes(10);
            DateTimeOffset expiresAt = now.Add(expiration);

            // Create some items
            QueueItem<TestClass> qi1 = new QueueItem<TestClass>(key, 3, tc1, lease, leaseUntil, expiresAt, now, 2);
            QueueItem<TestClass> qi1a = new QueueItem<TestClass>(key, 3, tc1, lease, leaseUntil, expiresAt, now, 2);
            QueueItem<TestClass> qi2 = new QueueItem<TestClass>(key, 3, tc2, lease, leaseUntil, expiresAt, now, 2);
            object o = qi1a;

            // Test Equals codes paths.
            Assert.IsFalse(qi1.Equals(null));
            Assert.IsFalse(qi1.Equals("Not equal string"));
            Assert.IsTrue(qi1.Equals(o));
            Assert.IsTrue(qi1.Equals(qi1a));
            Assert.IsFalse(qi1.Equals(qi2));

            // Test equals/not equals operators.
            Assert.IsFalse(qi1 == qi2);
            Assert.IsFalse(qi1 != qi1a);
            Assert.IsTrue(qi1 != qi2);
            Assert.IsTrue(qi1 == qi1a);
        }
    }
}
