// ------------------------------------------------------------
//  <copyright file="Component_RetryPolicy.cs" Company="Microsoft Corporation">
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// </copyright>
// ------------------------------------------------------------

namespace QueueServiceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Threading;
    using System.Threading.Tasks;
    using System;
    using QuickService.Common.ErrorHandling;
    using System.Diagnostics;

    /// <summary>
    /// RetryPolicy tests require a running application and fabric cluster.
    /// </summary>
    /// <remarks>This must be run on a box that has Service Fabric installed and running the application.</remarks>
    [TestClass]
    public class RetryPolicy_ComponentTests
    {
        [TestMethod]
        public void FabricClientRetryOperationShouldTimeout()
        {
            FabricClientRetryPolicy rp = new FabricClientRetryPolicy(5, new FixedWaitingPolicy(TimeSpan.FromMilliseconds(10)), AllErrorsTransientDetectionStrategy.Instance);
            Stopwatch sp = Stopwatch.StartNew();
            bool operationCanceled = false;
            int attempts = 0;
            try
            {
                rp.ExecuteWithRetriesAsync((ct) => Task.Run(async () =>
                {
                    attempts++;
                    await Task.Delay(Timeout.Infinite, ct);
                }), TimeSpan.FromMilliseconds(50), CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (TimeoutException)
            {
                Assert.IsTrue(sp.Elapsed < TimeSpan.FromSeconds(5), sp.Elapsed.ToString());
                operationCanceled = true;
            }
            Assert.AreEqual(5, attempts);
            Assert.AreEqual(true, operationCanceled);
        }
    }
}
