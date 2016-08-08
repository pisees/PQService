// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.Common.ErrorHandling
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Configures how to notify the caller when retry happen on a call without result
    /// </summary>
    public interface IRetryNotificationPolicy
    {
        /// <summary>
        /// Exception is caught while performing an operation under RetryPolicy
        /// </summary>
        /// <param name="shouldRetry">False in case this is last attempt or this is not a transient error, otherwise True</param>
        /// <param name="e">Exception caught</param>
        void OnException(bool shouldRetry, Exception e);
    }

    /// <summary>
    /// Configures how to notify the caller when retry happen on a call with result
    /// </summary>
    /// <typeparam name="TResult">The type of the result expected by the operation.</typeparam>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Reviewed. Suppression is OK here.")]
    public interface IRetryNotificationPolicy<TResult> : IRetryNotificationPolicy
    {
        /// <summary>
        /// Error condition is detected while performing an operation under RetryPolicy
        /// </summary>
        /// <param name="shouldRetry">False in case this is last attempt or this is not a transient error, otherwise True</param>
        /// <param name="result">Result which was detected as failure based on ITransientErrorDetectionStrategy instance</param>
        void OnErrorCondition(bool shouldRetry, TResult result);
    }

    /// <summary>
    /// Configures how to notify the caller when retry happen on a call without result
    /// </summary>
    public interface IRetryNotificationPolicyAsync
    {
        /// <summary>
        /// Exception is caught while performing an operation under RetryPolicy
        /// </summary>
        /// <param name="shouldRetry">False in case this is last attempt or this is not a transient error, otherwise True</param>
        /// <param name="e">Exception caught</param>
        /// <param name="cancellationToken">Cancellation token one should honor as part of RetryNotificationPolicy</param>
        /// <returns>Return an instance of Task</returns>
        Task OnExceptionAsync(bool shouldRetry, Exception e, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Configures how to notify the caller when retry happen on a call with result
    /// </summary>
    /// <typeparam name="TResult">The type of the result expected by the operation.</typeparam>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Reviewed. Suppression is OK here.")]
    public interface IRetryNotificationPolicyAsync<TResult> : IRetryNotificationPolicyAsync
    {
        /// <summary>
        /// Error condition is detected while performing an operation under RetryPolicy
        /// </summary>
        /// <param name="shouldRetry">False in case this is last attempt or this is not a transient error, otherwise True</param>
        /// <param name="result">Result which was detected as failure based on ITransientErrorDetectionStrategy instance</param>
        /// <param name="cancellationToken">Cancellation token one should honor as part of RetryNotificationPolicy</param>
        /// <returns>Return an instance of Task</returns>
        Task OnErrorConditionAsync(bool shouldRetry, TResult result, CancellationToken cancellationToken);
    }
}
