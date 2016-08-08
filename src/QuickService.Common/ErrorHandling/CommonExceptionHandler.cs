// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Diagnostics;
using QuickService.Common.Diagnostics;

namespace QuickService.Common.ErrorHandling
{
    /// <summary>
    /// Handles common server exceptions.
    /// </summary>
    public static class CommonExceptionHandler
    {
        /// <summary>
        /// Outputs inner exceptions to a Debug trace.
        /// </summary>
        /// <param name="ex">AggregateException instance.</param>
        public static void OutputInnerExceptions(AggregateException ex)
        {
            foreach (Exception inner in ex.InnerExceptions)
            {
                Debug.WriteLine($"AggregateException InnerException: {inner.Message} from {inner.StackTrace}");
            }
        }

        /// <summary>
        /// Outputs inner exceptions to an event source.
        /// </summary>
        /// <param name="ex">AggregateException instance.</param>
        /// <param name="es">IMinimalEventSource instance.</param>
        public static void OutputInnerExceptions(AggregateException ex, IMinimalEventSource es)
        {
            foreach (Exception inner in ex.InnerExceptions)
            {
                es.Error($"AggregateException InnerException: {inner.Message} from {inner.StackTrace}");
            }
        }
    }
}
