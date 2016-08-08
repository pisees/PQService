// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using QuickService.QueueClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using QuickService.Common;
using System.Web.Http;
using System.Threading.Tasks;
using System.Linq;

namespace QueueServiceConsole
{
    /// <summary>
    /// Runs multi threaded write tests.
    /// </summary>
    public sealed class WriteTest<T> : RunTest
        where T : IEquatable<T>
    {
        const string c_ListenerName = "OwinListener";
        const string serviceUri = "fabric:/PriorityQueueSample/PriorityQueueService";

        private CancellationToken _cancellationToken = default(CancellationToken);
        private Func<T> _createInstance = null;
        private int _priorityCount = 0;

        /// <summary>
        /// Name of this test.
        /// </summary>
        protected override string Name { get { return nameof(WriteTest<T>); } }

        /// <summary>
        /// ReadTest Constructor.
        /// </summary>
        /// <param name="batchSize">Size of the batch to process.</param>
        /// <param name="threads">Number of threads to run concurrently.</param>
        /// <param name="requests">Number of requests to process.</param>
        /// <param name="outputFileName">Name of the file to output the results to.</param>
        /// <param name="cancellationToken">CanellationToken instance.</param>
        public WriteTest(int batchSize, int threads, long requests, string outputFileName, Func<T> create,  CancellationToken cancellationToken)
            : base(threads, requests, outputFileName)
        {
            Guard.ArgumentNotNull(create, nameof(create));
            _batchSize = batchSize;
            _createInstance = create;

            // Create a queue client and retrieve the number of priorities.
            HttpQueueClient<T> qc = new HttpQueueClient<T>(new Uri(serviceUri), c_ListenerName);
            _priorityCount = qc.PriorityCountAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Executes the write test.
        /// </summary>
        public async Task RunAsync(CancellationToken token = default(CancellationToken))
        {
            Console.WriteLine("Starting WriteTest. ");

            if (_priorityCount <= 0)
            {
                Console.WriteLine($"Priority count is invalid {_priorityCount}.");
                return;
            }

            Stopwatch sw = Stopwatch.StartNew();

            if (await ExecuteTestsAsync(sw, TimeSpan.FromDays(7), WriteThreadProc))
            {
                OutputCallTrackingData(sw.ElapsedMilliseconds);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("WriteTest Failed.");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// Write test thread procedure.
        /// </summary>
        /// <param name="data">AutoResetEvent instance to signal when complete.</param>
        internal void WriteThreadProc(object data)
        {
            AutoResetEvent evt = (AutoResetEvent)data;

            HttpQueueClient<T> qc = new HttpQueueClient<T>(new Uri(serviceUri), c_ListenerName);

            // Check that cancellation isn't requested and we are not at the requested count.
            while ((_requestCount < _maxRequests) && (false == _cancellationToken.IsCancellationRequested))
            {
                try
                {
                    // Create a list of items to add.
                    List<T> addItems = new List<T>();
                    for(int i = 0; i < _batchSize; i++)
                    {
                        addItems.Add(_createInstance());
                    }

                    int queue = RandomThreadSafe.Instance.Next(0, _priorityCount);

                    Stopwatch swCall = Stopwatch.StartNew();
                    IEnumerable<QueueItem<T>> items = qc.EnqueueAsync(addItems, cancellationToken: _cancellationToken).GetAwaiter().GetResult();
                    swCall.Stop();

                    // Add the number of items added successfully.
                    Interlocked.Add(ref _successfulRequests, items.Count());

                    // Track the call time.
                    TrackCallData(swCall.ElapsedMilliseconds);
                }
                catch(TimeoutException) { Interlocked.Increment(ref _timedoutRequests); }
                catch(HttpResponseException ex)
                {
                    Console.WriteLine($"WriteTest.WriteThreadProc exception {ex.GetType().Name}: {ex.Message}, {ex.Response.StatusCode}, {ex.Response.ReasonPhrase}, {ex.Response.RequestMessage.RequestUri.AbsoluteUri}, {ex.StackTrace}");
                }
                catch (Exception ex) { Console.WriteLine($"WriteTest.WriteThreadProc exception {ex.GetType().Name}: {ex.Message}, {ex.StackTrace}"); }

                // Increment the request count.
                Interlocked.Increment(ref _requestCount);
                if (0 == _requestCount % 100)
                    Console.Write("\rTotal written: {0,-15:N0}", _requestCount);
            }

            // Signal this thread has completed.
            evt.Set();
        }
    }
}
