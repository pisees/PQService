// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.Common.ErrorHandling
{
    using System;

    /// <summary>
    /// The fixed time waiting policy.
    /// </summary>
    /// <remarks>
    /// High throughput applications should typically use an ExponentialBackoffWaitingPolicy.
    /// However, for user-facing applications such as websites you may want to consider a FixedWaitingPolicy or LinearWaitingPolicy to maintain the responsiveness of the UI.
    /// </remarks>
    public sealed class FixedWaitingPolicy : WaitingPolicy
    {
        #region Constants

        /// <summary>
        /// The default policy with fixed 1 second waiting
        /// </summary>
        public static readonly FixedWaitingPolicy Default = new FixedWaitingPolicy(TimeSpan.FromSeconds(1));

        #endregion

        #region Fields

        /// <summary>
        /// The fixed wait time.
        /// </summary>
        private readonly TimeSpan m_fixedWaitTime;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the <see cref="FixedWaitingPolicy"/> type.
        /// </summary>
        /// <param name="fixedWaitTime">The fixed amount of time to wait, cannot be negative.</param>
        public FixedWaitingPolicy(TimeSpan fixedWaitTime)
        {
            // Check the pre-conditions
            Guard.ArgumentNotNegativeValue(fixedWaitTime.Ticks, "fixedWaitTime");
            
            // Assign the fields
            m_fixedWaitTime = fixedWaitTime;
        }

        #endregion

        #region Interface implementation

        /// <summary>
        /// Computes the wait time for the specified attempt.
        /// </summary>
        /// <param name="attemptCount">Number of attempts.</param>
        /// <returns>TimeSpan containing the duration of the wait time.</returns>
        public override TimeSpan ComputeWaitTime(int attemptCount)
        {
            return m_fixedWaitTime;
        }

        #endregion
    }
}
