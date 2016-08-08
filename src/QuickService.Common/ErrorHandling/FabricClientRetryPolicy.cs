// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.Common.ErrorHandling
{
    using System;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A policy to retry an operation which has no return value (basically an Action)
    /// </summary>
    public class FabricClientRetryPolicy
    {
        #region Constants

        /// <summary>
        /// The default value for maximum number of times an operation should be retried
        /// </summary>
        public static int DefaultMaxAttempts = 5;

        /// <summary>
        /// The default retry policy using the <see cref="FixedWaitingPolicy"/> and detecting all exceptions as transient errors (except OperationCanceledException). Uses local FabricClient.
        /// </summary>
        public static readonly FabricClientRetryPolicy DefaultFixed = new FabricClientRetryPolicy(DefaultMaxAttempts, FixedWaitingPolicy.Default, AllErrorsTransientDetectionStrategy.Instance);

        #endregion

        #region Construction

        /// <summary>
        /// Creates a new instance of the <see cref="FabricClientRetryPolicy"/> type.
        /// </summary>
        /// <param name="maxAttempts">The maximum number of times an operation should be retried, must be positive.</param>
        /// <param name="waitingPolicy">The waiting policy to use, cannot be null.</param>
        /// <param name="defaultErrorDetectionStrategy">The default error detection strategy to use, cannot be null.</param>
        /// <param name="useFastRetriesForTesting">A flag indicating if we should use fast retries for testing, default is False</param>
        /// <exception cref="ArgumentNullException">If some of the non-nullable arguments are null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="maxAttempts"/> argument is not positive.</exception>
        public FabricClientRetryPolicy(int maxAttempts, WaitingPolicy waitingPolicy, ITransientErrorDetectionStrategy defaultErrorDetectionStrategy, bool useFastRetriesForTesting = false)
        {
            // Check the pre-conditions
            Guard.ArgumentNotZeroOrNegativeValue(maxAttempts, nameof(maxAttempts));
            Guard.ArgumentNotNull(waitingPolicy, nameof(waitingPolicy));
            Guard.ArgumentNotNull(defaultErrorDetectionStrategy, nameof(defaultErrorDetectionStrategy));

            // Assign the fields
            Client = CreateClient();
            MaxAttempts = maxAttempts;
            WaitingPolicy = waitingPolicy;
            DefaultErrorDetectionStrategy = defaultErrorDetectionStrategy;
            UseFastRetriesForTesting = useFastRetriesForTesting;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="FabricClientRetryPolicy"/> type.
        /// </summary>
        /// <param name="settings"><see cref="FabricClientSettings"/> instance.</param>
        /// <param name="maxAttempts">The maximum number of times an operation should be retried, must be positive.</param>
        /// <param name="waitingPolicy">The waiting policy to use, cannot be null.</param>
        /// <param name="defaultErrorDetectionStrategy">The default error detection strategy to use, cannot be null.</param>
        /// <param name="useFastRetriesForTesting">A flag indicating if we should use fast retries for testing, default is False</param>
        /// <exception cref="ArgumentNullException">If some of the non-nullable arguments are null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="maxAttempts"/> argument is not positive.</exception>
        public FabricClientRetryPolicy(FabricClientSettings settings, int maxAttempts, WaitingPolicy waitingPolicy, ITransientErrorDetectionStrategy defaultErrorDetectionStrategy, bool useFastRetriesForTesting = false)
        {
            // Check the pre-conditions
            Guard.ArgumentNotNull(settings, nameof(settings));
            Guard.ArgumentNotZeroOrNegativeValue(maxAttempts, nameof(maxAttempts));
            Guard.ArgumentNotNull(waitingPolicy, nameof(waitingPolicy));
            Guard.ArgumentNotNull(defaultErrorDetectionStrategy, nameof(defaultErrorDetectionStrategy));

            // Assign the fields
            _fabricClientSettings = settings;
            Client = CreateClient();
            MaxAttempts = maxAttempts;
            WaitingPolicy = waitingPolicy;
            DefaultErrorDetectionStrategy = defaultErrorDetectionStrategy;
            UseFastRetriesForTesting = useFastRetriesForTesting;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="FabricClientRetryPolicy"/> type.
        /// </summary>
        /// <param name="credentials"><see cref="SecurityCredentials"/> to use when connecting to the endpoint.</param>
        /// <param name="hostEndpoints">Defines the set of Gateway addresses the <see cref="FabricClient"/> can use to connect to the cluster.</param>
        /// <param name="maxAttempts">The maximum number of times an operation should be retried, must be positive.</param>
        /// <param name="waitingPolicy">The waiting policy to use, cannot be null.</param>
        /// <param name="defaultErrorDetectionStrategy">The default error detection strategy to use, cannot be null.</param>
        /// <param name="useFastRetriesForTesting">A flag indicating if we should use fast retries for testing, default is False</param>
        /// <exception cref="ArgumentNullException">If some of the non-nullable arguments are null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If the <paramref name="maxAttempts"/> argument is not positive.</exception>
        public FabricClientRetryPolicy(SecurityCredentials credentials, string[] hostEndpoints, int maxAttempts, WaitingPolicy waitingPolicy, ITransientErrorDetectionStrategy defaultErrorDetectionStrategy, bool useFastRetriesForTesting = false)
        {
            // Check the pre-conditions
            Guard.ArgumentNotNull(credentials, nameof(credentials));
            Guard.ArgumentNotNull(hostEndpoints, nameof(hostEndpoints));
            Guard.ArgumentNotZeroOrNegativeValue(maxAttempts, nameof(maxAttempts));
            Guard.ArgumentNotNull(waitingPolicy, nameof(waitingPolicy));
            Guard.ArgumentNotNull(defaultErrorDetectionStrategy, nameof(defaultErrorDetectionStrategy));

            // Assign the fields
            _credentials = credentials;
            _hostEndpoints = hostEndpoints;
            Client = CreateClient();
            MaxAttempts = maxAttempts;
            WaitingPolicy = waitingPolicy;
            DefaultErrorDetectionStrategy = defaultErrorDetectionStrategy;
            UseFastRetriesForTesting = useFastRetriesForTesting;
        }

        #endregion

        #region Fields

        /// <summary>
        /// FabricClientSettings instance.
        /// </summary>
        private FabricClientSettings _fabricClientSettings = null;

        /// <summary>
        /// <see cref="SecurityCredentials"/> to use when connecting to the endpoint.
        /// </summary>
        private SecurityCredentials _credentials = null;

        /// <summary>
        /// Defines the set of Gateway addresses the <see cref="FabricClient"/> can use to connect to the cluster.
        /// </summary>
        private string[] _hostEndpoints = null;

        /// <summary>
        /// Gets the maximum number of times an operation should be attempted.
        /// </summary>
        public readonly int MaxAttempts;

        /// <summary>
        /// Gets the maximum number of times an operation should be attempted.
        /// </summary>
        public readonly WaitingPolicy WaitingPolicy;

        /// <summary>
        /// Gets the default error detection strategy.
        /// </summary>
        public readonly ITransientErrorDetectionStrategy DefaultErrorDetectionStrategy;

        /// <summary>
        /// Gets a flag indicating if we should use fast retries for testing.
        /// </summary>
        public readonly bool UseFastRetriesForTesting;

        /// <summary>
        /// FabricClient instance.
        /// </summary>
        public FabricClient Client { get; private set; }

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates the <see cref="FabricClient"/> based on the initialized parameters.
        /// </summary>
        /// <returns><see cref="FabricClient"/> instance.</returns>
        private FabricClient CreateClient()
        {
            if ((null != _fabricClientSettings) && (null != _credentials) && (null != _hostEndpoints) && (_hostEndpoints.Length > 0))
            {
                return new FabricClient(_credentials, _fabricClientSettings, _hostEndpoints);
            }
            else if ((null != _credentials) && (null != _hostEndpoints) && (_hostEndpoints.Length > 0))
            {
                return new FabricClient(_credentials, _hostEndpoints);
            }
            else if (null != _fabricClientSettings)
            {
                return new FabricClient(_fabricClientSettings);
            }

            return new FabricClient();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Executes an operation with retries.
        /// </summary>
        /// <param name="operationToAttempt">The operation, cannot be null.</param>
        /// <param name="errorDetectionStrategy">The error detection strategy to use instead of the default one.</param>
        /// <param name="notificationPolicy">The notification policy to use.</param>
        /// <exception cref="ArgumentNullException">If some of the non-nullable arguments are null.</exception>
        /// <exception cref="Exception">Any exception thrown by the <paramref name="operationToAttempt"/> after all the attempts have been exhausted.</exception>
        public void ExecuteWithRetries(Action operationToAttempt, ITransientErrorDetectionStrategy errorDetectionStrategy = null, IRetryNotificationPolicy notificationPolicy = null)
        {
            // Check the pre-conditions
            Guard.ArgumentNotNull(operationToAttempt, nameof(operationToAttempt));

            errorDetectionStrategy = errorDetectionStrategy ?? DefaultErrorDetectionStrategy;
            for (var attemptCount = 0; attemptCount < MaxAttempts; ++attemptCount)
            {
                try
                {
                    // Execute the operation and return.
                    operationToAttempt();
                    return;
                }
                catch (ObjectDisposedException) { Client = CreateClient(); }      // Renew the client and retry.
                catch (FabricObjectClosedException) { Client = CreateClient(); }  // Renew the client and retry.
                catch (Exception ex)
                {
                    // Check if it is transient exception, we always treat timeout exceptions as transient
                    bool isTransient = IsTimeoutException(ex) || errorDetectionStrategy.IsTransientException(ex);
                    bool shouldRetry = ShouldRetry(attemptCount, isTransient);
                    // Notify the caller if we are requested to do so
                    if (notificationPolicy != null)
                    {
                        notificationPolicy.OnException(shouldRetry, ex);
                    }

                    if (!shouldRetry)
                    {
                        throw;
                    }
                }
                if (!UseFastRetriesForTesting)
                {
                    // Compute the delay and wait
                    var delay = WaitingPolicy.ComputeWaitTime(attemptCount);
                    Thread.Sleep(delay);
                }
            } // for
            throw new InvalidOperationException("This cannot be reached");
        }

        /// <summary>
        /// Executes an async operation (action with no result) with retries.
        /// </summary>
        /// <param name="operationToAttempt">The operation, cannot be null.</param>
        /// <param name="timeout">Optional timeout for the operation for single retry iteration, can be TimeSpan.Zero or Timeout.Infinite which means no timeout</param>
        /// <param name="cancellationToken">Optional token to cancel the waiting policy, operation and notification.</param>
        /// <param name="errorDetectionStrategy">Optional error detection strategy to use instead of the default one.</param>
        /// <param name="notificationPolicy">Optional notification policy to use.</param>
        /// <returns>The future for the result of the operation.</returns>
        /// <exception cref="ArgumentNullException">If some of the non-nullable arguments are null.</exception>
        /// <exception cref="TimeoutException">If the operation was timeout in all retries.</exception>
        /// <exception cref="OperationCanceledException">If the operation was canceled.</exception>
        /// <exception cref="Exception">Any exception thrown by the <paramref name="operationToAttempt"/> after all the attempts have been exhausted.</exception>
        public async Task ExecuteWithRetriesAsync(
            Func<CancellationToken, Task> operationToAttempt,
            TimeSpan timeout = default(TimeSpan),
            CancellationToken cancellationToken = default(CancellationToken),
            ITransientErrorDetectionStrategy errorDetectionStrategy = null,
            IRetryNotificationPolicyAsync notificationPolicy = null)
        {
            // Check the pre-conditions
            Guard.ArgumentNotNull(operationToAttempt, nameof(operationToAttempt));
            Guard.ArgumentNotNegativeValue(timeout.Ticks, nameof(timeout));

            errorDetectionStrategy = errorDetectionStrategy ?? DefaultErrorDetectionStrategy;
            for (var attemptCount = 0; attemptCount < MaxAttempts; ++attemptCount)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Construct timeout and cancellation linked token - it's important to timely Dispose
                // the "timeout" cancellation source (as well), because internally, it captures the 
                // (current) thread's (CLR) execution context (synchronization context, logical call 
                // context, etc.), so everything "tied" to these structures (like logging context, for 
                // example, "chained" to a CallContext slot) would have a strong reference and will not 
                // be eligible for garbage collection (note that these are managed resources!). These 
                // would be released eventually when the timeout expires, but we should not rely on that 
                // (if timeout passed in is considerable and the service is under pressure, we may easily 
                // run out of memory). 
                using (var timeoutCts = !IsNoTimeout(timeout) ? new CancellationTokenSource(timeout) : null)
                {
                    using (var timeoutAndCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(
                        cancellationToken, timeoutCts == null ? CancellationToken.None : timeoutCts.Token))
                    {
                        var timeoutAndCancellationToken = timeoutAndCancellationSource.Token;

                        try
                        {
                            // Execute the operation
                            await operationToAttempt(timeoutAndCancellationToken).ConfigureAwait(false);

                            // We are good to go
                            return;
                        }
                        catch (ObjectDisposedException) { Client = CreateClient(); }      // Renew the client and retry.
                        catch (FabricObjectClosedException) { Client = CreateClient(); }  // Renew the client and retry.
                        catch (Exception ex)
                        {
                            // cancellationToken is set should bail out
                            cancellationToken.ThrowIfCancellationRequested();

                            // Check if it is transient exception, we always treat timeout exceptions as transient
                            bool isTransient = IsTimeoutException(ex) || errorDetectionStrategy.IsTransientException(ex);
                            bool shouldRetry = ShouldRetry(attemptCount, isTransient);
                            // Notify the caller if we are requested to do so
                            if (notificationPolicy != null)
                            {
                                await notificationPolicy.OnExceptionAsync(shouldRetry, ex, cancellationToken);
                            }

                            if (!shouldRetry)
                            {
                                if (ex is OperationCanceledException) // We wrap OperationCanceledException/TaskCanceledException in TimeoutException to indicate that operation was timeod out
                                {
                                    throw new TimeoutException($"The operation has timed out after all {MaxAttempts} attempts. Each attempt took more than ${timeout.Milliseconds}ms.", ex);
                                }
                                else throw;
                            }
                        }
                    }  // using
                } // using

                if (!UseFastRetriesForTesting)
                {
                    // Compute the delay and wait
                    var delay = WaitingPolicy.ComputeWaitTime(attemptCount);

                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            } // for
            throw new InvalidOperationException("This cannot be reached");
        }

        /// <summary>
        /// Indicates if a retry should be attempted or not.
        /// </summary>
        /// <param name="attemptCount">Number of retries attempted.</param>
        /// <param name="isTransient">Indicates if this is transient or not.</param>
        /// <returns>True if retry should be attempted, otherwise false.</returns>
        protected bool ShouldRetry(int attemptCount, bool isTransient)
        {
            // Check if we need to retry for the n-th time
            return isTransient && attemptCount < (MaxAttempts - 1);
        }

        /// <summary>
        /// Indicates if this is a timeout exception.
        /// </summary>
        /// <param name="ex">Exception to evaluate.</param>
        /// <returns>True if this is a timeout exception, otherwise false.</returns>
        protected static bool IsTimeoutException(Exception ex)
        {
            // We don't need to check for TaskCanceledException because it inherits from OperationCanceledException
            return (ex is OperationCanceledException) || (ex is TimeoutException);
        }

        /// <summary>
        /// Indicates if there is time on the timeout.
        /// </summary>
        /// <param name="timeout">TimeSpan containing the timeout duration.</param>
        /// <returns>True if time is remaining, otherwise false.</returns>
        protected static bool IsNoTimeout(TimeSpan timeout)
        {
            return ((timeout == TimeSpan.Zero) || (timeout.Milliseconds == Timeout.Infinite));
        }

        #endregion
    }
}
