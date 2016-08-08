// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.Common
{
    using System;
    using System.Threading;

    /// <summary>
    /// Thread safe random class.
    /// </summary>
    /// <remarks>If your app calls Random methods from multiple threads, you must use a synchronization object to
    /// ensure that only one thread can access the random number generator at a time. If you don't ensure that the 
    /// Random object is accessed in a thread-safe way, calls to methods that return random numbers return 0.
    /// <seealso cref="System.Random"/></remarks>
    public sealed class RandomThreadSafe
    {
        /// <summary>
        /// Random class instance per thread (TLS) and with lazy creation
        /// </summary>
        private static readonly ThreadLocal<Lazy<Random>> _random = new ThreadLocal<Lazy<Random>>(() => new Lazy<Random>(() => new Random(Thread.CurrentThread.ManagedThreadId + DateTime.UtcNow.Millisecond + DateTime.UtcNow.DayOfYear)));

        /// <summary>
        /// Static instance of Random for this thread.
        /// </summary>
        public static Random Instance
        {
            get
            {
                Random result = _random.Value.Value;
                if (null == result) throw new OutOfMemoryException("Could not allocate random number generator."); 
                return result;
            }
        }
    }
}
