// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;

namespace QuickService.Common.Diagnostics
{
    /// <summary>
    /// Count per second structure used to track the service RPS.
    /// </summary>
    public struct CountPerSecond
    {
        internal readonly Int64 _tick;
        internal readonly Int64 _count;

        /// <summary>
        /// CountPerSecond Constructor.
        /// </summary>
        /// <param name="tick">Tick count when the structure was initialized.</param>
        /// <param name="count">Count.</param>
        internal CountPerSecond(Int64 tick, Int64 count)
        {
            _tick = tick == 0 ? DateTime.Now.Ticks : tick;
            _count = count;
        }

        /// <summary>
        /// Default counts per second.
        /// </summary>
        public static readonly CountPerSecond Zero = new CountPerSecond(0, 0);

        /// <summary>
        /// Increments the request count by N.
        /// </summary>
        /// <param name="cps">Current CountPerSecond instance.</param>
        /// <param name="addend">Number of counts to increment.</param>
        /// <returns>A new CountPerSecond instance with the count incremented by the value of the addend.</returns>
        public static CountPerSecond operator +(CountPerSecond cps, Int32 addend) => new CountPerSecond(cps._tick, cps._count + addend);

        /// <summary>
        /// Get the latest request per second value.
        /// </summary>
        /// <returns>Long integer containing the number of count per second.</returns>
        public Int64 GetLatest() => _count / Math.Max(1, (DateTime.Now.Ticks - _tick) / TimeSpan.TicksPerSecond);

        /// <summary>
        /// Allows casting of a CountPerSecond instance to a long value.
        /// </summary>
        /// <param name="cps"></param>
        public static implicit operator Int64(CountPerSecond cps) => cps.GetLatest();

        /// <summary>
        /// Get the string value of the count per second.
        /// </summary>
        /// <returns>String instance containing the current number of count per second.</returns>
        public override string ToString() => GetLatest().ToString();
    }
}
