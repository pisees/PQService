// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.Common.ErrorHandling
{
    using System;
    using QuickService.Common;

    /// <summary>
    /// The exponential back-off waiting policy.
    /// </summary>
    /// <remarks>
    /// High throughput applications should typically use an exponential back-off strategy.
    /// However, for user-facing applications such as websites you may want to consider a FixedWaitingPolicy or LinearWaitingPolicy to maintain the responsiveness of the UI.
    /// </remarks>
    public sealed class ExponentialBackoffWaitingPolicy : WaitingPolicy
    {
        #region Constants

        /// <summary>
        /// The default exponential back-off waiting policy with MinBackoff = 2ms, MaxBackoff = 5000ms and DeltaBackoff = 10ms
        /// </summary>
        public static readonly ExponentialBackoffWaitingPolicy Default = new ExponentialBackoffWaitingPolicy(TimeSpan.FromMilliseconds(2), TimeSpan.FromMilliseconds(5000), TimeSpan.FromMilliseconds(10));

        #endregion

        #region Properties

        /// <summary>
        /// Gets the minimum back-off time.
        /// </summary>
        public readonly TimeSpan MinBackoff;

        /// <summary>
        /// Gets the maximum back-off time.
        /// </summary>
        public readonly TimeSpan MaxBackoff;

        /// <summary>
        /// Gets the time value which will be used for calculating a random delta in the exponential delay between retries
        /// </summary>
        public readonly TimeSpan DeltaBackoff;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new instance of the <see cref="WaitingPolicy"/> type.
        /// </summary>
        /// <param name="minBackoff">The minimum back-off time, must be non negative.</param>
        /// <param name="maxBackoff">The maximum back-off time, must be non negative and not smaller than <paramref name="minBackoff"/>.</param>
        /// <param name="deltaBackoff">
        /// The time value which will be used for calculating a random delta in the exponential delay between retries, must be non negative.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// If the <paramref name="minBackoff"/> or the <paramref name="maxBackoff"/> or the <paramref name="deltaBackoff"/> argument is
        /// out of the allowed range.
        /// </exception>
        public ExponentialBackoffWaitingPolicy(TimeSpan minBackoff, TimeSpan maxBackoff, TimeSpan deltaBackoff)
        {
            // Check the pre-conditions
            Guard.ArgumentNotNegativeValue(minBackoff.Ticks, nameof(minBackoff));
            Guard.ArgumentNotNegativeValue(maxBackoff.Ticks, nameof(minBackoff));
            Guard.ArgumentNotNegativeValue(deltaBackoff.Ticks, nameof(minBackoff));

            // Assign the fields
            MinBackoff = minBackoff;
            MaxBackoff = maxBackoff;
            DeltaBackoff = deltaBackoff;
        }

        #endregion

        /// <summary>
        /// Computes the wait time for the specified attempt.
        /// </summary>
        /// <param name="attemptCount">Number of attempts.</param>
        /// <returns>TimeSpan containing the duration of the wait time.</returns>
        public override TimeSpan ComputeWaitTime(int attemptCount)
        {
            return ComputeExponentialBackoff(MinBackoff, MaxBackoff, DeltaBackoff, attemptCount + 1);
        }

        /// <summary>
        /// Calculates the random exponential back-off interval using the specified minimum, maximum and delta parameters.
        /// </summary>
        /// <param name="minBackoff">The minimum back-off time.</param>
        /// <param name="maxBackoff">The maximum back-off time.</param>
        /// <param name="delta">The time value which will be used for calculating a random delta in the exponential delay.</param>
        /// <param name="increment">The increment that defines the progression in the exponential curve.</param>
        /// <returns>The time value containing the computed random exponential back-off interval.</returns>
        /// <remarks>
        /// This method is taken directly from the CloudFX library (we own that library so no licensing issues).
        /// </remarks>
        public static TimeSpan ComputeExponentialBackoff(TimeSpan minBackoff, TimeSpan maxBackoff, TimeSpan delta, int increment)
        {
            Guard.ArgumentNotNegativeValue(increment, nameof(increment));
            Guard.ArgumentNotNegativeValue(minBackoff.Ticks, nameof(minBackoff));
            Guard.ArgumentNotNegativeValue(maxBackoff.Ticks, nameof(maxBackoff));
            Guard.ArgumentNotNegativeValue(delta.Ticks, nameof(delta));
            Guard.ArgumentNotGreaterThan(minBackoff.TotalMilliseconds, maxBackoff.TotalMilliseconds, nameof(minBackoff));

            var deltaBackoff = Math.Abs(Math.Pow(2.0, increment) - 1.0) * RandomThreadSafe.Instance.Next((int)(delta.TotalMilliseconds * 0.8), (int)(delta.TotalMilliseconds * 1.2));
            var backoffInterval = (int)Math.Min(checked(minBackoff.TotalMilliseconds + deltaBackoff), maxBackoff.TotalMilliseconds);

            return TimeSpan.FromMilliseconds(backoffInterval);
        }
    }
}
