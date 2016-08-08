// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QueueServiceTests
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using QuickService.Common.ErrorHandling;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.Fabric;


    ///////////////////////////////////////////////////////////////////
    // TODO: 
    // 1) Add tests for notifications.
    // 2) Add CircuitBreaker tests.
    ///////////////////////////////////////////////////////////////////

    /// <summary>
    /// Summary description for UnitTest_RetryPolicy
    /// </summary>
    [TestClass]
    public class UnitTest_RetryPolicy
    {
        #region Retry Policy Tests

        [TestMethod]
        public void RetryDelayShouldBeCancellable()
        {
            RetryPolicy rp = new RetryPolicy(1000, ExponentialBackoffWaitingPolicy.Default, AllErrorsTransientDetectionStrategy.Instance);
            Stopwatch sp = Stopwatch.StartNew();
            bool operationCanceled = false;
            int attempts = 0;
            try
            {
                using (CancellationTokenSource cts = new CancellationTokenSource(50))
                {
                    rp.ExecuteWithRetriesAsync((ct) => Task.Run(async () =>
                    {
                        attempts++;
                        await Task.Delay(Timeout.Infinite, ct);
                    }), TimeSpan.Zero, cts.Token).GetAwaiter().GetResult();
                }
            }
            catch (OperationCanceledException)
            {
                Assert.IsTrue(sp.Elapsed < TimeSpan.FromSeconds(5), sp.Elapsed.ToString());
                operationCanceled = true;
            }
            Assert.AreEqual(1, attempts);
            Assert.AreEqual(true, operationCanceled);
        }

        [TestMethod]
        public void RetryOperationShouldTimeout()
        {
            RetryPolicy rp = new RetryPolicy(5, new FixedWaitingPolicy(TimeSpan.FromMilliseconds(10)), AllErrorsTransientDetectionStrategy.Instance);
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

        [TestMethod]
        public void RetryDelayShouldBeCancellableT()
        {
            RetryPolicy rp = new RetryPolicy<string>(1000, ExponentialBackoffWaitingPolicy.Default, AllErrorsTransientDetectionStrategy<string>.Instance);
            Stopwatch sp = Stopwatch.StartNew();
            bool operationCanceled = false;
            int attempts = 0;
            try
            {
                using (CancellationTokenSource cts = new CancellationTokenSource(50))
                {
                    rp.ExecuteWithRetriesAsync((ct) => Task.Run(async () =>
                    {
                        attempts++;
                        await Task.Delay(Timeout.Infinite, ct);
                    }), TimeSpan.Zero, cts.Token).GetAwaiter().GetResult();
                }
            }
            catch (OperationCanceledException)
            {
                Assert.IsTrue(sp.Elapsed < TimeSpan.FromSeconds(5), sp.Elapsed.ToString());
                operationCanceled = true;
            }
            Assert.AreEqual(1, attempts);
            Assert.AreEqual(true, operationCanceled);
        }

        [TestMethod]
        public void RetryOperationShouldTimeoutT()
        {
            RetryPolicy rp = new RetryPolicy<string>(5, new FixedWaitingPolicy(TimeSpan.FromMilliseconds(10)), AllErrorsTransientDetectionStrategy<string>.Instance);
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

        #endregion

        #region RetryPolicy Exception Tests

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public void RetryTimeoutFailed()
        {
            RetryPolicy rp = new RetryPolicy(5, new FixedWaitingPolicy(TimeSpan.FromMilliseconds(10)), AllErrorsTransientDetectionStrategy.Instance);
            int attempts = 0;
            rp.ExecuteWithRetries(() =>
            {
                attempts++;
                throw new OperationCanceledException();
            });
        }

        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public async Task RetryTimeoutFailedAsync()
        {
            RetryPolicy rp = new RetryPolicy(5, new FixedWaitingPolicy(TimeSpan.FromMilliseconds(10)), AllErrorsTransientDetectionStrategy.Instance);
            int attempts = 0;
            await rp.ExecuteWithRetriesAsync((ct) =>
            {
                attempts++;
                throw new OperationCanceledException();
            });
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]  
        public void RetryZeroArgumentOutOfRangeException()
        {
            RetryPolicy rp = new RetryPolicy(0, new FixedWaitingPolicy(TimeSpan.FromMilliseconds(10)), AllErrorsTransientDetectionStrategy.Instance);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void RetryMinusArgumentOutOfRangeException()
        {
            RetryPolicy rp = new RetryPolicy(int.MinValue, new FixedWaitingPolicy(TimeSpan.FromMilliseconds(10)), AllErrorsTransientDetectionStrategy.Instance);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RetryNullWaitingPolicy()
        {
            RetryPolicy rp = new RetryPolicy(5, null, AllErrorsTransientDetectionStrategy.Instance);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RetryNullTransientDetectionPolicy()
        {
            RetryPolicy rp = new RetryPolicy(5, new FixedWaitingPolicy(TimeSpan.FromMilliseconds(10)), null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RetryNullOperation()
        {
            RetryPolicy rp = new RetryPolicy(5, new FixedWaitingPolicy(TimeSpan.FromMilliseconds(10)), AllErrorsTransientDetectionStrategy.Instance);
            rp.ExecuteWithRetries(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task RetryNullOperationAsync()
        {
            RetryPolicy rp = new RetryPolicy(5, new FixedWaitingPolicy(TimeSpan.FromMilliseconds(10)), AllErrorsTransientDetectionStrategy.Instance);
            await rp.ExecuteWithRetriesAsync(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public async Task RetryNegativeTimeoutOperationAsync()
        {
            RetryPolicy rp = new RetryPolicy(5, new FixedWaitingPolicy(TimeSpan.FromMilliseconds(10)), AllErrorsTransientDetectionStrategy.Instance);
            await rp.ExecuteWithRetriesAsync(async (ct) =>
            {
                await Task.Delay(1);
            }, TimeSpan.FromMilliseconds(-1));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void RetryZeroArgumentOutOfRangeExceptionT()
        {
            RetryPolicy rp = new RetryPolicy<string>(0, new FixedWaitingPolicy(TimeSpan.FromMilliseconds(10)), AllErrorsTransientDetectionStrategy<string>.Instance);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void RetryMinusArgumentOutOfRangeExceptionT()
        {
            RetryPolicy rp = new RetryPolicy<string>(int.MinValue, new FixedWaitingPolicy(TimeSpan.FromMilliseconds(10)), AllErrorsTransientDetectionStrategy<string>.Instance);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RetryNullWaitingPolicyT()
        {
            RetryPolicy rp = new RetryPolicy<string>(5, null, AllErrorsTransientDetectionStrategy<string>.Instance);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RetryNullTransientDetectionPolicyT()
        {
            RetryPolicy rp = new RetryPolicy<string>(5, new FixedWaitingPolicy(TimeSpan.FromMilliseconds(10)), null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RetryNullOperationT()
        {
            RetryPolicy rp = new RetryPolicy<string>(5, new FixedWaitingPolicy(TimeSpan.FromMilliseconds(10)), AllErrorsTransientDetectionStrategy<string>.Instance);
            rp.ExecuteWithRetries(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task RetryNullOperationAsyncT()
        {
            RetryPolicy rp = new RetryPolicy<string>(5, new FixedWaitingPolicy(TimeSpan.FromMilliseconds(10)), AllErrorsTransientDetectionStrategy<string>.Instance);
            await rp.ExecuteWithRetriesAsync(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public async Task RetryNegativeTimeoutOperationAsyncT()
        {
            RetryPolicy rp = new RetryPolicy<string>(5, new FixedWaitingPolicy(TimeSpan.FromMilliseconds(10)), AllErrorsTransientDetectionStrategy<string>.Instance);
            await rp.ExecuteWithRetriesAsync(async (ct) =>
            {
                await Task.Delay(1);
            }, TimeSpan.FromMilliseconds(-1));
        }

        #endregion

        #region Fabric Client Retry Policy Tests

        [TestMethod]
        public void FabricClientRetryPolicyShouldBeCancelled()
        {
            FabricClientRetryPolicy rp = new FabricClientRetryPolicy(5, FixedWaitingPolicy.Default, AllErrorsTransientDetectionStrategy.Instance);
            Stopwatch sp = Stopwatch.StartNew();
            bool operationCanceled = false;
            int attempts = 0;
            try
            {
                using (CancellationTokenSource cts = new CancellationTokenSource(50))
                {
                    rp.ExecuteWithRetriesAsync((ct) => Task.Run(async () =>
                    {
                        attempts++;
                        await Task.Delay(Timeout.Infinite, ct);
                    }), TimeSpan.Zero, cts.Token).GetAwaiter().GetResult();
                }
            }
            catch (OperationCanceledException)
            {
                Assert.IsTrue(sp.Elapsed < TimeSpan.FromSeconds(5), sp.Elapsed.ToString());
                operationCanceled = true;
            }
            Assert.IsNotNull(rp.Client);
            Assert.AreEqual(1, attempts);
            Assert.AreEqual(true, operationCanceled);
        }

        [TestMethod]
        public void FabricClientRetryPolicyOperationShouldTimeout()
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
            Assert.IsNotNull(rp.Client);
            Assert.AreEqual(5, attempts);
            Assert.AreEqual(true, operationCanceled);
        }

        [TestMethod]
        public void FabricClientRetryPolicy_2_ShouldBeCancelled()
        {
            FabricClientSettings fcs = new FabricClientSettings() { ConnectionInitializationTimeout = TimeSpan.FromSeconds(5) };

            FabricClientRetryPolicy rp = new FabricClientRetryPolicy(fcs, 5, FixedWaitingPolicy.Default, AllErrorsTransientDetectionStrategy.Instance);
            Stopwatch sp = Stopwatch.StartNew();
            bool operationCanceled = false;
            int attempts = 0;
            try
            {
                using (CancellationTokenSource cts = new CancellationTokenSource(50))
                {
                    rp.ExecuteWithRetriesAsync((ct) => Task.Run(async () =>
                    {
                        attempts++;
                        await Task.Delay(Timeout.Infinite, ct);
                    }), TimeSpan.Zero, cts.Token).GetAwaiter().GetResult();
                }
            }
            catch (OperationCanceledException)
            {
                Assert.IsTrue(sp.Elapsed < TimeSpan.FromSeconds(5), sp.Elapsed.ToString());
                operationCanceled = true;
            }
            Assert.IsNotNull(rp.Client);
            Assert.AreEqual(1, attempts);
            Assert.AreEqual(true, operationCanceled);
        }

        [TestMethod]
        public void FabricClientRetryPolicy_2_OperationShouldTimeout()
        {
            FabricClientSettings fcs = new FabricClientSettings() { ConnectionInitializationTimeout = TimeSpan.FromSeconds(5) };

            FabricClientRetryPolicy rp = new FabricClientRetryPolicy(fcs, 5, new FixedWaitingPolicy(TimeSpan.FromMilliseconds(10)), AllErrorsTransientDetectionStrategy.Instance);
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
            Assert.IsNotNull(rp.Client);
            Assert.AreEqual(5, attempts);
            Assert.AreEqual(true, operationCanceled);
        }

        [TestMethod]
        public void FabricClientRetryPolicy_3_ShouldBeCancelled()
        {
            SecurityCredentials sc = new NoneSecurityCredentials();
            string[] ep = new string[] { "tcp://tempuri.org" };

            FabricClientRetryPolicy rp = new FabricClientRetryPolicy(sc, ep, 5, FixedWaitingPolicy.Default, AllErrorsTransientDetectionStrategy.Instance);
            Stopwatch sp = Stopwatch.StartNew();
            bool operationCanceled = false;
            int attempts = 0;
            try
            {
                using (CancellationTokenSource cts = new CancellationTokenSource(50))
                {
                    rp.ExecuteWithRetriesAsync((ct) => Task.Run(async () =>
                    {
                        attempts++;
                        await Task.Delay(Timeout.Infinite, ct);
                    }), TimeSpan.Zero, cts.Token).GetAwaiter().GetResult();
                }
            }
            catch (OperationCanceledException)
            {
                Assert.IsTrue(sp.Elapsed < TimeSpan.FromSeconds(5), sp.Elapsed.ToString());
                operationCanceled = true;
            }
            Assert.IsNotNull(rp.Client);
            Assert.AreEqual(1, attempts);
            Assert.AreEqual(true, operationCanceled);
        }

        [TestMethod]
        public void FabricClientRetryPolicy_3_OperationShouldTimeout()
        {
            SecurityCredentials sc = new NoneSecurityCredentials();
            string[] ep = new string[] { "tcp://tempuri.org" };

            FabricClientRetryPolicy rp = new FabricClientRetryPolicy(sc, ep, 5, new FixedWaitingPolicy(TimeSpan.FromMilliseconds(10)), AllErrorsTransientDetectionStrategy.Instance);
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
            Assert.IsNotNull(rp.Client);
            Assert.AreEqual(5, attempts);
            Assert.AreEqual(true, operationCanceled);
        }

        [TestMethod]
        public void FabricClientRetryPolicyClientRetryOperationShouldTimeout()
        {
            FabricClientRetryPolicy rp = new FabricClientRetryPolicy(5, new FixedWaitingPolicy(TimeSpan.FromMilliseconds(10)), AllErrorsTransientDetectionStrategy.Instance);
            Stopwatch sp = Stopwatch.StartNew();
            bool operationCanceled = false;
            int attempts = 0;
            FabricClient.QueryClient qc = null;

            try
            {
                // Force the object to be disposed.
                rp.Client.Dispose();

                rp.ExecuteWithRetriesAsync((ct) => Task.Run(async () =>
                {
                    attempts++;
                    qc = rp.Client.QueryManager;
                    await Task.Delay(Timeout.Infinite, ct);
                }), TimeSpan.FromMilliseconds(50), CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (TimeoutException)
            {
                Assert.IsTrue(sp.Elapsed < TimeSpan.FromSeconds(5), sp.Elapsed.ToString());
                operationCanceled = true;
            }
            Assert.IsNotNull(rp.Client);
            Assert.AreEqual(5, attempts);
            Assert.AreEqual(true, operationCanceled);
        }

        [TestMethod]
        public void FabricClientRetryPolicyClientRetryShouldBeCancelled()
        {
            FabricClientRetryPolicy rp = new FabricClientRetryPolicy(5, FixedWaitingPolicy.Default, AllErrorsTransientDetectionStrategy.Instance);
            Stopwatch sp = Stopwatch.StartNew();
            bool operationCanceled = false;
            int attempts = 0;
            FabricClient.QueryClient qc = null;

            try
            {
                // Force the object to be disposed.
                rp.Client.Dispose();

                using (CancellationTokenSource cts = new CancellationTokenSource(50))
                {
                    rp.ExecuteWithRetriesAsync((ct) => Task.Run(async () =>
                    {
                        attempts++;
                        qc = rp.Client.QueryManager;
                        await Task.Delay(Timeout.Infinite, ct);
                    }), TimeSpan.Zero, cts.Token).GetAwaiter().GetResult();
                }
            }
            catch (OperationCanceledException)
            {
                Assert.IsTrue(sp.Elapsed < TimeSpan.FromSeconds(5), sp.Elapsed.ToString());
                operationCanceled = true;
            }
            Assert.IsNotNull(rp.Client);
            Assert.IsNotNull(qc);
            Assert.AreEqual(1, attempts);
            Assert.AreEqual(true, operationCanceled);
        }

        #endregion
    }
}
