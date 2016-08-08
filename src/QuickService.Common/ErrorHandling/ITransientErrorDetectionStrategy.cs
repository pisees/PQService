// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.Common.ErrorHandling
{
    using System;

    /// <summary>
    /// The interface of a strategy to detect transient errors for operations that do not return a value.
    /// </summary>
    public interface ITransientErrorDetectionStrategy
    {
        /// <summary>
        /// Checks if an exception should be considered as a transient error.
        /// </summary>
        /// <param name="ex">The exception, cannot be null.</param>
        /// <returns>True if the error should be considered transient.</returns>
        /// <remarks>This function should not throw any exception, make sure you handle the case when result is null</remarks>
        bool IsTransientException(Exception ex);
    }

    /// <summary>
    /// The interface of a strategy to detect transient errors for operations that return a value.
    /// </summary>
    /// <typeparam name="TResult">The type of the result of the operation.</typeparam>
    public interface ITransientErrorDetectionStrategy<in TResult> : ITransientErrorDetectionStrategy
    {
        /// <summary>
        /// Checks if a result of an operation corresponds to a failure situation.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>True if the result should be considered as a failure.</returns>
        /// <remarks>This function should not throw any exception, make sure you handle the case when result is null</remarks>
        bool IsFailure(TResult result);

        /// <summary>
        /// Checks if a result of an operation corresponds to a transient failure situation.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <returns>True if the result should be considered as a transient failure.</returns>
        /// <remarks>This function should not throw any exception, make sure you handle the case when result is null</remarks>
        bool IsTransientFailure(TResult result);
    }
}
