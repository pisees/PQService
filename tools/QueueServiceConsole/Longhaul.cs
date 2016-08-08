// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using QuickService.Common;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace QueueServiceConsole
{
    public sealed class Longhaul<T> : RunTest
        where T : IEquatable<T>
    {
        private Uri _serviceUri = null;
        private int _faultProgress = 0;
        private string _listnerName = null;
        private Func<T> _createProc = null;

        protected override string Name { get { return "Longhaul"; } }

        public Longhaul(int threads, long maxRequests, Uri serviceName, string listener, Func<T> createProc) 
            : base(threads, maxRequests)
        {
            _serviceUri = serviceName;
            _listnerName = listener;
            _createProc = createProc;
        }

        public Task RunAsync(TimeSpan duration, CancellationToken token)
        {
            // Can't run for more than 24 days because the maximum number of milliseconds in a Int32 is 24.8 days.
            Guard.ArgumentInRange(duration.TotalDays, 0, 24, nameof(duration));

            DateTime endTime = DateTime.Now.Add(duration);
            Stopwatch sw = new Stopwatch();

            // Start a failure test scenario.
            FaultTest ft = new FaultTest(FaultTest.TestType.Failover, duration);
            Task ftt = ft.RunAsync(_serviceUri, ProgressHandler);

            // Start a read test scenario.
            ReadTest<T> readTest = new ReadTest<T>(1, _numThreads, _maxRequests, _outputFileName, token);
            Task rtt = ExecuteTestsAsync(sw, duration, readTest.ReadThreadProc);
            long readTestCount = 0;
            long rtd = sw.ElapsedMilliseconds;

            // Start a write test scenario.
            WriteTest<T> writeTest = new WriteTest<T>(1, _numThreads, _maxRequests, _outputFileName, _createProc, token);
            Task wtt = ExecuteTestsAsync(sw, duration, writeTest.WriteThreadProc);
            long writeTestCount = 0;
            long wtd = sw.ElapsedMilliseconds;

            // Handle task completion until the duration is exceeded.
            while (endTime > DateTime.Now)
            {
                token.ThrowIfCancellationRequested();

                int timeoutMS = (int)(endTime.Subtract(DateTime.Now).TotalMilliseconds);
                try
                {
                    // Wait for a task to complete.
                    if (-1 == Task.WaitAny(new[] { ftt, rtt, wtt }, timeoutMS, token))
                    {
                        Console.WriteLine("Timed out waiting for the tasks to complete.");
                    }

                    if (TaskStatus.RanToCompletion == ftt.Status)
                    {
                        ftt = ft.RunAsync(_serviceUri, ProgressHandler);
                    }

                    if (TaskStatus.RanToCompletion == rtt.Status)        // rtt - output read data to the local file and start another read test.
                    {
                        readTest.OutputPerformenceRunFile(sw.ElapsedMilliseconds - rtd, CalculateRPS(_maxRequests, sw.ElapsedMilliseconds, rtd));
                        rtt = ExecuteTestsAsync(sw, duration, readTest.ReadThreadProc);
                        rtd = sw.ElapsedMilliseconds;
                        readTestCount++;
                    }

                    if (TaskStatus.RanToCompletion == wtt.Status)        // wtt - output write data to the local file and start another write test.
                    {
                        writeTest.OutputPerformenceRunFile(sw.ElapsedMilliseconds - wtd, CalculateRPS(_maxRequests, sw.ElapsedMilliseconds, wtd));
                        wtt = ExecuteTestsAsync(sw, duration, writeTest.WriteThreadProc);
                        wtd = sw.ElapsedMilliseconds;
                        writeTestCount++;
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Exception: {0} at {1}", ex.Message, ex.StackTrace);
                }

                Console.Write($"\rMinutes remaining: {Math.Max(0, (endTime.Subtract(DateTime.Now)).TotalMinutes):N2} Fault progress: {_faultProgress:N0}%\tRead test count: {readTestCount:N}\tWrite test count: {writeTestCount:N}");
            }

            return Task.FromResult(true);
        }

        /// <summary>
        /// Calculates the request per second based on the number of requests, current elapsed time and the starting time.
        /// </summary>
        /// <param name="requests"></param>
        /// <param name="currentMS"></param>
        /// <param name="startMS"></param>
        /// <returns></returns>
        private long CalculateRPS(long requests, long currentMS, long startMS)
        {
            long ms = currentMS - startMS;
            if (ms > 0)
                return requests / ms;
            return requests;
        }

        private void ProgressHandler(object sender, ProgressChangedEventArgs args)
        {
            Debug.WriteLine($"Progress: {args.ProgressPercentage} State: {args.UserState.ToString()}");
            _faultProgress = args.ProgressPercentage;
        }
    }
}
