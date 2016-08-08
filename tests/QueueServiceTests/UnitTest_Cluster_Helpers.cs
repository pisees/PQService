// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QueueServiceTests
{
    using System;
    using QuickService.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Fabric;
    [TestClass]
    public class ClusterHelpers_Tests
    {
        Uri serviceUri = new Uri("fabric:/PriorityQueueSample/PriorityQueueService");

        [TestMethod]
        public void TestClass_GetPartitionCount_FabricClient()
        {
            FabricClient client = new FabricClient();
            int count = ClusterHelpers.GetPartitionCountAsync(serviceUri).GetAwaiter().GetResult();
            Assert.AreEqual(2, count);
        }
    }
}
