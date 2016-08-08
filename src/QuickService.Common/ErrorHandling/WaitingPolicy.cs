// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.Common.ErrorHandling
{
    using System;

    /// <summary>
    /// Configures how the retry logic will behave in case of failures and,
    /// specifically, how much time it will wait between subsequent retries.
    /// </summary>
    public abstract class WaitingPolicy
    {
        #region Methods

        /// <summary>
        /// Computes the wait time for the next attempt.
        /// </summary>
        /// <param name="attemptCount">The number of the current attempt which faulted (zero based)</param>
        /// <returns>The time to wait.</returns>
        public abstract TimeSpan ComputeWaitTime(int attemptCount);

        #endregion
    }
}
