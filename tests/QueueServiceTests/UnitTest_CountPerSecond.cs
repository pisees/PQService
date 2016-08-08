// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuickService.Common.Rest;
using Microsoft.ServiceFabric.Services.Communication.Client;
using System.Net;
using QuickService.Common.Diagnostics;

namespace QueueServiceTests
{
    [TestClass]
    public class UnitTest_CountPerSecond
    {
        [TestMethod]
        public void CountPerSecond_Constructor()
        {
            long ticks = DateTime.Now.Ticks;
            var cps = new CountPerSecond(ticks, 0);
            Assert.IsNotNull(cps);
            Assert.AreEqual(0, cps._count);
            Assert.AreEqual(ticks, cps._tick);
            Assert.AreEqual<long>(0L, cps);
            Assert.AreEqual(0L, cps.GetLatest());
            Assert.AreEqual("0", cps.ToString());
        }

        [TestMethod]
        public void CountPerSecond_Zero()
        {
            long ticks = DateTime.Now.Ticks;
            var cps = CountPerSecond.Zero;
            Assert.IsNotNull(cps);
            Assert.AreEqual(0, cps._count);
            Assert.AreNotEqual(0, cps._tick);
            Assert.IsTrue(cps._tick - ticks < TimeSpan.TicksPerMillisecond);
        }

        [TestMethod]
        public void CountPerSecond_IncrementOperator()
        {
            long ticks = DateTime.Now.Ticks;
            var cps = new CountPerSecond(ticks, 0);
            Assert.AreEqual<long>(0L, cps);
            Assert.AreEqual(0, cps._count);
            Assert.AreEqual(ticks, cps._tick);

            cps += 1;
            Assert.AreEqual(1, cps._count);

            cps += 5;
            Assert.AreEqual(6, cps._count);
        }

        [TestMethod]
        public void CountPerSecond_FiveSecond()
        { 
            const int c_seconds = 5;

            var cps = CountPerSecond.Zero;
            Assert.AreEqual<long>(0L, cps);
            long startTick = cps._tick;
            long endTick = startTick + TimeSpan.FromSeconds(c_seconds).Ticks;
            long count = 0;

            while (DateTime.Now.Ticks < endTick)
            {
                cps += 1;
                count++;
            }

            Assert.AreEqual(count / c_seconds, cps.GetLatest());
            Console.WriteLine($"Actual: {count / c_seconds} CountsPerSecond: {cps.GetLatest()}");
        }

        [TestMethod]
        public void CountPerSecond_GetLatest()
        {
            var cps = new CountPerSecond(DateTime.Now.Subtract(TimeSpan.FromMinutes(5)).Ticks, 0L);
            Assert.AreEqual<long>(0L, cps);

            cps += 3000;
            Assert.AreEqual<long>(10L, cps);

            cps = new CountPerSecond(DateTime.Now.Subtract(TimeSpan.FromMinutes(5)).Ticks, 3000L);
            Assert.AreEqual<long>(10L, cps);
        }

    }
}
