// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuickService.Common.Diagnostics;

namespace QueueServiceTests
{
    [TestClass]
    public class UnitTest_AverageLatency
    {
        [TestMethod]
        public void AverageLatency_Constructor()
        {
            var latency = new AverageLatency();
            Assert.IsNotNull(latency);
            Assert.AreEqual(0, latency._count);
            Assert.AreEqual(0, latency._latencyTotal);

            latency = AverageLatency.Zero;
            Assert.IsNotNull(latency);
            Assert.AreEqual(0, latency._count);
            Assert.AreEqual(0, latency._latencyTotal);
            Assert.AreEqual<long>(0, latency);
            Assert.AreEqual("0", latency.ToString());
        }

        [TestMethod]
        public void AverageLatency_IncrementOperator()
        {
            var latency = new AverageLatency();
            latency += 100;
            Assert.AreEqual(1, latency._count);
            Assert.AreEqual(100, latency._latencyTotal);
            Assert.AreEqual<long>(100, latency);
            Assert.AreEqual(100, latency.GetLatest());
            Assert.AreEqual("100", latency.ToString());

            latency += 50;
            Assert.AreEqual(2, latency._count);
            Assert.AreEqual(150, latency._latencyTotal);
            Assert.AreEqual<long>(75, latency);
        }
    }
}
