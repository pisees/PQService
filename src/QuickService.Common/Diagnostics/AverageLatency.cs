// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;

namespace QuickService.Common.Diagnostics
{
    /// <summary>
    /// Tracks the average class latency.
    /// </summary>
    public struct AverageLatency
    {
        internal readonly Int64 _latencyTotal;
        internal readonly Int64 _count;

        /// <summary>
        /// AverageLatency counter.
        /// </summary>
        /// <param name="latency">Total of the latency during the interval.</param>
        /// <param name="count">Number of latency samples.</param>
        internal AverageLatency(Int64 latency, Int64 count)
        {
            _latencyTotal = latency;
            _count = count;
        }

        /// <summary>
        /// Default average latency.
        /// </summary>
        public static readonly AverageLatency Zero = new AverageLatency();

        /// <summary>
        /// Adds a particular latency to the average latency for the period.
        /// </summary>
        /// <param name="avg">Current AverageLatency instance.</param>
        /// <param name="latency">Latency measurement to add to the average.</param>
        /// <returns>AverageLatency instance.</returns>
        public static AverageLatency operator +(AverageLatency avg, Int64 latency) => new AverageLatency(avg._latencyTotal + latency, avg._count + 1);

        /// <summary>
        /// Converts an AverageLatency into an integer value in milliseconds.
        /// </summary>
        /// <param name="avg">Current AverageLatency structure.</param>
        public static implicit operator Int64(AverageLatency avg) => avg.GetLatest();

        /// <summary>
        /// Returns the current average latency.
        /// </summary>
        /// <returns>Int64 number containing the average latency over the interval.</returns>
        public Int64 GetLatest() => _latencyTotal / Math.Max(1, _count);

        /// <summary>
        /// Gets the string value of the average latency.
        /// </summary>
        /// <returns>String containing the average latency.</returns>
        public override string ToString() => GetLatest().ToString();
    }
}
