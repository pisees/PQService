// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.Common.ErrorHandling
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// The strategy that considers all errors as transient except from <see cref="OperationCanceledException"/>s.
    /// </summary>
    public sealed class AllErrorsTransientDetectionStrategy : ITransientErrorDetectionStrategy
    {
        /// <summary>
        /// The shared instance.
        /// </summary>
        public static readonly AllErrorsTransientDetectionStrategy Instance = new AllErrorsTransientDetectionStrategy();

        /// <summary>
        /// Indicates if the exception is a transient exception or not.
        /// </summary>
        /// <param name="ex">Exception to evaluate.</param>
        /// <returns>True if transient, otherwise false.</returns>
        public bool IsTransientException(Exception ex)
        {
            return !(ex is OperationCanceledException);
        }
    }

    /// <summary>
    /// The strategy that considers all errors as transient except from <see cref="OperationCanceledException"/>s and considers
    /// all returned values as a success.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Reviewed. Suppression is OK here.")]
    public sealed class AllErrorsTransientDetectionStrategy<TResult> : ITransientErrorDetectionStrategy<TResult>
    {
        /// <summary>
        /// The shared instance.
        /// </summary>
        public static readonly AllErrorsTransientDetectionStrategy<TResult> Instance = new AllErrorsTransientDetectionStrategy<TResult>();

        /// <summary>
        /// Indicates if the exception is a transient exception or not.
        /// </summary>
        /// <param name="ex">Exception to evaluate.</param>
        /// <returns>True if transient, otherwise false.</returns>
        public bool IsTransientException(Exception ex)
        {
            return !(ex is OperationCanceledException);
        }

        /// <summary>
        /// Indicates if the exception is a failure exception or not.
        /// </summary>
        /// <param name="result">Exception to evaluate.</param>
        /// <returns>True if failure, otherwise false.</returns>
        public bool IsFailure(TResult result)
        {
            return false;
        }

        /// <summary>
        /// Indicates if the result is a transient failure or not.
        /// </summary>
        /// <param name="result">Result to evaluate.</param>
        /// <returns>True if transient, otherwise false.</returns>
        public bool IsTransientFailure(TResult result)
        {
            return false;
        }
    }
}
