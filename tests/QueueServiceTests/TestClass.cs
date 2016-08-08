// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;

namespace QueueServiceTests
{
    public sealed class TestClass : IEquatable<TestClass>
    {
        public int i_value { get; set; }

        public double d_value { get; set; }

        public string s_value { get; set; }

        public TestClass()
        {
        }

        /// <summary>
        /// Checks for instance equality.
        /// </summary>
        /// <param name="other">Instance to compare.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public bool Equals(TestClass other)
        {
            if (this.i_value != other.i_value)
                return false;
            if (this.d_value != other.d_value)
                return false;

            return this.s_value == other.s_value;
        }

        #region IEqutable<TestClass>

        /// <summary>
        /// Indicates if the object passed and the instance are equal.
        /// </summary>
        /// <param name="item">Item to compare.</param>
        /// <returns>True if equal, otherwise false.</returns>
        public bool Equal(TestClass item)
        {
            if (i_value != item.i_value)
                return false;
            if (d_value != item.d_value)
                return false;
            if (s_value != item.s_value)
                return false;

            return true;
        }

        #endregion
    }
}
