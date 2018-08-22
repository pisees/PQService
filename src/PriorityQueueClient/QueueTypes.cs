// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("QueueServiceTests")]

namespace QuickService.QueueClient
{
    /// <summary>
    /// Special queue constants.
    /// </summary>
    public struct QueueType
    {
        /// <summary>
        /// QueueType value.
        /// </summary>
        private int _value;

        /// <summary>
        /// QueueType constructor accepting an integer value.
        /// </summary>
        /// <param name="value">Value of the queue type.</param>
        public QueueType(int value) { _value = value; }

        /// <summary>
        /// Cast operator from QueueType to Int32.
        /// </summary>
        /// <param name="value">Integer value to convert.</param>
        public static implicit operator int(QueueType value) { return value._value; }

        /// <summary>
        /// Cast operator from Int32 to QueueType.
        /// </summary>
        /// <param name="value">QueueType value to convert.</param>
        public static implicit operator QueueType(int value) { return new QueueType(value); }

        #region Constants

        /// <summary>
        /// Constant representing the first queue, 0.
        /// </summary>
        public const int FirstQueue = 0;

        /// <summary>
        /// Constant representing all of the queues from 0 to N.
        /// </summary>
        public const int AllQueues = -1;

        /// <summary>
        /// Constant representing the last queue, N.
        /// </summary>
        public const int LastQueue = -1;

        /// <summary>
        /// Constant representing the dictionary containing leased items.
        /// </summary>
        public const int LeaseQueue = -2;

        /// <summary>
        /// Constant representing the dictionary containing expired items.
        /// </summary>
        public const int ExpiredQueue = -3;

        /// <summary>
        /// Constant representing the dictionary containing all items.
        /// </summary>
        public const int ItemQueue = -4;

        #endregion
    };
}
