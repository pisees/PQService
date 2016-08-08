// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using QuickService.QueueClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace QueueServiceConsole
{
    /// <summary>
    /// Runs multi threaded read tests.
    /// </summary>
    public sealed class ReadTest<T> : RunTest
        where T : IEquatable<T>
    {
        const string c_ListenerName = "OwinListener";
        const string serviceUri = "fabric:/PriorityQueueSample/PriorityQueueService";

        private CancellationToken _cancellationToken = default(CancellationToken);

        /// <summary>
        /// Name of this test.
        /// </summary>
        protected override string Name { get { return nameof(ReadTest<T>); } }

        /// <summary>
        /// ReadTest Constructor.
        /// </summary>
        /// <param name="batchSize">Size of the batch to process.</param>
        /// <param name="threads">Number of threads to run concurrently.</param>
        /// <param name="requests">Number of requests to process.</param>
        /// <param name="outputFileName">Name of the file to output the results to.</param>
        /// <param name="cancellationToken">CanellationToken instance.</param>
        public ReadTest(int batchSize, int threads, long requests, string outputFileName, CancellationToken cancellationToken)
            : base(threads, requests, outputFileName)
        {
            _batchSize = batchSize;
        }

        /// <summary>
        /// Executes the read test.
        /// </summary>
        public async Task RunAsync(CancellationToken token = default(CancellationToken))
        {
            Console.WriteLine("Starting ReadTest. ");

            Stopwatch sw = Stopwatch.StartNew();

            if (await ExecuteTestsAsync(sw, TimeSpan.FromDays(7), ReadThreadProc).ConfigureAwait(false))
            {
                OutputCallTrackingData(sw.ElapsedMilliseconds);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ReadTest Failed.");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Read test thread procedure.
        /// </summary>
        /// <param name="data">AutoResetEvent instance to signal when complete.</param>
        internal void ReadThreadProc(object data)
        {
            AutoResetEvent evt = (AutoResetEvent)data;

            // Create a queue client.
            HttpQueueClient<T> qc = new HttpQueueClient<T>(new Uri(serviceUri), c_ListenerName);

            // Check that cancellation isn't requested and we are not at the requested count.
            while ((_requestCount < _maxRequests) && (false == _cancellationToken.IsCancellationRequested))
            {
                try
                {

                    Stopwatch swCall = Stopwatch.StartNew();
                    IEnumerable<QueueItem<T>> items = qc.DequeueAsync(_batchSize, cancellationToken: _cancellationToken).GetAwaiter().GetResult();
                    swCall.Stop();

                    // Track the call time.
                    TrackCallData(swCall.ElapsedMilliseconds);

                    // Build the list of leases to release.
                    List<PopReceipt> keys = new List<PopReceipt>(_batchSize);
                    foreach (QueueItem<T> item in items)
                    {
                        keys.Add(item.Key);
                    }

                    // Were any items returned?
                    if (0 == keys.Count)
                    {
                        Interlocked.Increment(ref _notFoundOrConflict);
                    }
                    else // Release the held leases for each item.
                    {
                        IEnumerable<bool> results = qc.ReleaseLeaseAsync(keys, _cancellationToken).GetAwaiter().GetResult();
                        foreach (bool bResult in results)
                        {
                            if (bResult)
                                Interlocked.Increment(ref _successfulRequests);
                        }
                    }
                }
                catch(TimeoutException) { Interlocked.Increment(ref _timedoutRequests); }
                catch (Exception ex) { Console.WriteLine($"ReadTest.ReadThreadProc exception {ex.GetType().Name}: {ex.Message}, {ex.StackTrace}"); }

                // Increment the request count.
                Interlocked.Increment(ref _requestCount);
                if (0 == _requestCount % 100)
                    Console.Write("\rTotal Read: {0,-15:N0}", _requestCount);
            }

            // Signal this thread has completed.
            evt.Set();
        }
    }
}
