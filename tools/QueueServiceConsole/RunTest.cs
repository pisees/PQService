// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace QueueServiceConsole
{
    /// <summary>
    /// Base class for running tests.
    /// </summary>
    public abstract class RunTest
    {
        /// <summary>
        /// Delegate to create an instance.
        /// </summary>
        /// <typeparam name="T">Type of item to create.</typeparam>
        /// <returns>An instance of type T.</returns>
        public delegate T CreateInstanceDelegate<T>();

        /// <summary>
        /// Name of the output file.
        /// </summary>
        protected string _outputFileName = "TestOutput.csv";

        // Performance tracking members.
        protected int _batchSize = 1;
        protected int _numThreads = 0;
        protected long _maxRequests = 0L;
        protected long _requestCount = 0L;
        protected long _successfulRequests = 0L;
        protected long _notFoundOrConflict = 0L;
        protected long _throttledRequests = 0L;
        protected long _timedoutRequests = 0L;
        protected int[] _callPerfData = new int[1001];         // Tracks time spent in calls. This is hard coded in output data methods at the end of the file.

        protected abstract string Name { get; }

        /// <summary>
        /// RunTest constructor.
        /// </summary>
        protected RunTest(int threads, long maxRequests, string outputFileName = null)
        {
            _numThreads = threads;
            _maxRequests = maxRequests;
            _outputFileName = outputFileName ?? _outputFileName;
        }

        /// <summary>
        /// Executes tests.
        /// </summary>
        /// <param name="sw">Stopwatch instance.</param>
        /// <param name="proc">Thread proc to execute.</param>
        /// <returns>Indicator of success. True if successful, otherwise false.</returns>
        protected Task<bool> ExecuteTestsAsync(Stopwatch sw, TimeSpan maxDuration, ParameterizedThreadStart proc)
        {
            ResetTrackingData();

            try
            {
                // Create and start the threads.
                Thread[] threads = new Thread[_numThreads];
                AutoResetEvent[] events = new AutoResetEvent[_numThreads];
                for (int i = 0; i < _numThreads; i++)
                {
                    threads[i] = new Thread(proc);
                    events[i] = new AutoResetEvent(false);
                    threads[i].Start(events[i]);
                }

                // Start the stopwatch and wait for all threads to exit.
                sw.Start();
                bool result = WaitHandle.WaitAll(events, maxDuration);
                return Task.FromResult(result);
            }
            catch (Exception ex) { Console.WriteLine($"{ex.GetType().Name}. {ex.Message}"); throw; }
        }

        /// <summary>
        /// Tracks the number of calls by duration in milliseconds.
        /// </summary>
        /// <param name="value">Duration of the call in milliseconds.</param>
        protected void TrackCallData(long value)
        {
            if (value < 1000)
                Interlocked.Increment(ref _callPerfData[value]);
            else
                Interlocked.Increment(ref _callPerfData[1000]);
        }

        /// <summary>
        /// Resets the tracking data counters.
        /// </summary>
        protected void ResetTrackingData()
        {
            // Ensure tracking members are reset.
            _requestCount = 0L;
            _successfulRequests = 0L;
            _notFoundOrConflict = 0L;
            _throttledRequests = 0L;
            _timedoutRequests = 0L;

            for (int i = 0; i < _callPerfData.Length; i++)
            {
                _callPerfData[i] = 0;
            }
        }

        /// <summary>
        /// Outputs a summary of the call durations.
        /// </summary>
        protected void OutputCallTrackingData(long duration)
        {
            // If there were no requests made, there are no durations to track.
            if (0 == _maxRequests)
            {
                Console.WriteLine("{0} Total Duration {1}ms.", Name, duration);
                return;
            }

            const int numBuckets = 5;
            int currentBucket = 0;
            long total = 0;
            long runningTotal = 0;
            long meanDuration = 0;
            long medianDuration = 0;
            int lastNonZeroDuration = 0;
            long[] bc = new long[numBuckets];           // Duration counts per bucket.
            long[] bv = new long[numBuckets];           // Maximum duration value for this bucket.
            long countPerBucket = (_maxRequests - _callPerfData[1000]) / numBuckets;    // Exclude the items taking longer than 1 second.
            long elapsedSeconds = Math.Max(1, (duration / 1000));
            long rps = _maxRequests / elapsedSeconds;

            // Run through the list and divide into 5 buckets.
            for (int i = 0; i < _callPerfData.Length - 1; i++)
            {
                // Add the count for this bucket to the running total and total.
                total += _callPerfData[i];
                runningTotal += _callPerfData[i];

                // If the total is half of the number of requests, it's the median.
                if ((0 == medianDuration) && (total >= (_maxRequests / 2)))
                    medianDuration = i;

                // Add the total count for this duration to the mean.
                meanDuration += i * _callPerfData[i];

                // Track the last non-zero duration.
                if (_callPerfData[i] > 0)
                    lastNonZeroDuration = i;

                // If the running total is more than should be in a bucket, start on the next bucket.
                if ((runningTotal >= countPerBucket) || (i == 999))
                {
                    if (currentBucket < numBuckets)
                    {
                        bv[currentBucket] = lastNonZeroDuration;
                        bc[currentBucket] = runningTotal;
                        currentBucket++;
                    }
                    runningTotal = 0;
                }
            }

            Console.WriteLine("\n{0,-10}{1,-20}{2,-20}{3,-20}{4,-20}{5,-20}", Name.Substring(0,Math.Min(Name.Length, 10)), "Total", "Successful", "Conflict/Not Found", "Throttled", "Timeout");
            Console.WriteLine("Requests {0,-20:N0}{1,-20:N0}{2,-20:N0}{3,-20:N0}{4,-20:N0}", _maxRequests, _successfulRequests, _notFoundOrConflict, _throttledRequests, _timedoutRequests);
            Console.WriteLine("Profile: {0,-20:N0}{1,-20:N0}{2,-20:N0}{3,-20:N0}{4,-20:N0}", $"< {bv[0]}ms", $"< {bv[1]}ms", $"< {bv[2]}ms", $"< {bv[3]}ms", $"< {bv[4]}ms");
            Console.WriteLine("         {0,-20:N0}{1,-20:N0}{2,-20:N0}{3,-20:N0}{4,-20:N0}{5,-20:N0}", bc[0], bc[1], bc[2], bc[3], bc[4], _callPerfData[1000]);
            Console.WriteLine("Req/Sec: {0:F2}\tItems/sec: {1:F2}\tMean Duration: {2:F2}ms\tMedian Duration: {3}ms\tTotal Duration: {4}ms", rps, rps *_batchSize, meanDuration / _maxRequests, medianDuration, duration);

            // Output data to file.
            OutputPerformenceRunFile(duration, rps);
        }

        /// <summary>
        /// Output the data from the performance run to a file that can easily imported into Excel.
        /// </summary>
        /// <param name="duration"></param>
        internal void OutputPerformenceRunFile(long duration, long rps)
        {           
            using (StreamWriter writer = File.AppendText(_outputFileName))
            {
                // If this was just created, then add the file headers.
                if (0 == writer.BaseStream.Length)
                {
                    writer.Write("Date/Time,Test,Threads, Duration,RPS,Total Requests,Batch Size,Successful Requests,Conflict or Not Found,> 1000ms");
                    for (int i = 0; i < 1000; i++)
                    {
                        writer.Write("ms,");
                        writer.Write(i);
                    }
                    writer.WriteLine();
                }

                writer.Write("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}", DateTime.UtcNow, Name, _numThreads, duration, rps, _maxRequests, _batchSize, _successfulRequests, _notFoundOrConflict, _callPerfData[1000]);
                for (int i = 0; i < 1000; i++)
                {
                    writer.Write(",{0}", _callPerfData[i]);
                }

                writer.WriteLine();
                writer.Flush();
            }
        }
    }
}
