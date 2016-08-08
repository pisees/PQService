// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.Common.ErrorHandling
{
    using System;

    /// <summary>
    /// The linear interval waiting policy.
    /// </summary>
    /// <remarks>
    /// High throughput applications should typically use an ExponentialBackoffWaitingPolicy.
    /// However, for user-facing applications such as websites you may want to consider a FixedWaitingPolicy or LinearWaitingPolicy to maintain the responsiveness of the UI.
    /// </remarks>
    public sealed class LinearWaitingPolicy : WaitingPolicy
    {
        #region Constants

        /// <summary>
        /// The default linear interval waiting policy with initial interval of 50ms and increment of 100ms
        /// </summary>
        public static readonly LinearWaitingPolicy Default = new LinearWaitingPolicy(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(100));

        #endregion

        #region Properties

        /// <summary>
        /// The initial interval which will apply for the first retry.
        /// </summary>
        public TimeSpan InitialInterval { get; private set; }

        /// <summary>
        /// The incremental time value which will be used for calculating the progressive delay between retries.
        /// </summary>
        public TimeSpan Increment { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the <see cref="LinearWaitingPolicy"/> type.
        /// </summary>
        /// <param name="initialInterval">The initial interval which will apply for the first retry.</param>
        /// <param name="increment">The incremental time value which will be used for calculating the progressive delay between retries.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If the <paramref name="initialInterval"/> or the <paramref name="increment"/> argument is out of the allowed range.
        /// </exception>
        public LinearWaitingPolicy(TimeSpan initialInterval, TimeSpan increment)
        {
            // Check the pre-conditions
            Guard.ArgumentNotNegativeValue(initialInterval.Ticks, "initialInterval");
            Guard.ArgumentNotNegativeValue(increment.Ticks, "increment");

            // Assign the fields
            InitialInterval = initialInterval;
            Increment = increment;
        }

        #endregion

        /// <summary>
        /// Computes the wait time for the specified attempt.
        /// </summary>
        /// <param name="attemptCount">Number of attempts.</param>
        /// <returns>TimeSpan containing the duration of the wait time.</returns>
        public override TimeSpan ComputeWaitTime(int attemptCount)
        {
            return TimeSpan.FromMilliseconds(InitialInterval.TotalMilliseconds + attemptCount * Increment.TotalMilliseconds);
        }
    }
}
