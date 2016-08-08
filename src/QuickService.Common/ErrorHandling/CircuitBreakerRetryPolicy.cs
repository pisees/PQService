// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.Common.ErrorHandling
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A policy to retry an operation which has no return value (basically an Action)
    /// </summary>
    public class CircuitBreakerRetryPolicy : RetryPolicy
    {
        #region Constants

        /// <summary>
        /// The default policy detecting all exceptions as transient errors (except OperationCanceledException)
        /// </summary>
        public static readonly CircuitBreakerRetryPolicy Default = new CircuitBreakerRetryPolicy(AllErrorsTransientDetectionStrategy.Instance);

        #endregion

        #region Construction

        /// <summary>
        /// Creates a new instance of the <see cref="CircuitBreakerRetryPolicy"/> type.
        /// </summary>
        /// <param name="defaultErrorDetectionStrategy">The default error detection strategy to use, cannot be null.</param>
        /// <param name="waitingPolicy">ExponentialBackoffWaitingPolicy instance or null for the default policy.</param>
        /// <param name="useFastRetriesForTesting">A flag indicating if we should use fast retries for testing, default is False</param>
        /// <exception cref="ArgumentNullException">If some of the non-nullable arguments are null.</exception>
        public CircuitBreakerRetryPolicy(ITransientErrorDetectionStrategy defaultErrorDetectionStrategy, ExponentialBackoffWaitingPolicy waitingPolicy = null, bool useFastRetriesForTesting = false)
            : base(int.MaxValue, waitingPolicy ?? new ExponentialBackoffWaitingPolicy(TimeSpan.FromMilliseconds(2), TimeSpan.FromMinutes(10), TimeSpan.FromMilliseconds(10)), defaultErrorDetectionStrategy, useFastRetriesForTesting)
        {
        }

        #endregion
    }

    /// <summary>
    /// A policy to retry an operation which has return value (basically a Function)
    /// </summary>
    /// <typeparam name="TResult">The type of the operation result.</typeparam>
    public sealed class CircuitBreakerRetryPolicy<TResult> : RetryPolicy<TResult>
    {
        #region Construction

        /// <summary>
        /// Creates a new instance of the <see cref="CircuitBreakerRetryPolicy"/> type.
        /// </summary>
        /// <param name="defaultErrorDetectionStrategy">The default error detection strategy to use, cannot be null.</param>
        /// <param name="waitingPolicy">The waiting policy to use, cannot be null.</param>
        /// <param name="useFastRetriesForTesting">A flag indicating if we should use fast retries for testing, default is False</param>
        /// <exception cref="ArgumentNullException">If some of the non-nullable arguments are null.</exception>
        public CircuitBreakerRetryPolicy(ITransientErrorDetectionStrategy<TResult> defaultErrorDetectionStrategy, ExponentialBackoffWaitingPolicy waitingPolicy, bool useFastRetriesForTesting = false)
            : base(int.MaxValue, waitingPolicy, defaultErrorDetectionStrategy, useFastRetriesForTesting)
        {
        }

        #endregion
    }
}
