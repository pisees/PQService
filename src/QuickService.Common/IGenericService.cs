// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.Common
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Methods required by a service using the GenericXXXService classes.
    /// </summary>
    public interface IGenericService
    {
        /// <summary>
        /// Get the operation CancellationTokenSource instance to signal when an exit is requested.
        /// </summary>
        /// <returns>CancellationTokenSource instance for this set of operations.</returns>
        CancellationTokenSource GetCancellationTokenSource { get; }
    }
}
