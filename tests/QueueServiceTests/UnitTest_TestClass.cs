// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QueueServiceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestClass_UnitTest
    {
        TestClass tc0 = new TestClass();
        TestClass tc1 = new TestClass() { i_value = 1, d_value = 1.1, s_value = "one" };
        TestClass tc2 = new TestClass() { i_value = 2, d_value = 2.2, s_value = "two" };

        [TestMethod]
        public void TestClass_ConstructorTests()
        {
            Assert.AreEqual<int>(0, tc0.i_value);
            Assert.AreEqual<double>(0.0, tc0.d_value);
            Assert.AreEqual<string>(null, tc0.s_value);

            Assert.AreEqual<int>(1, tc1.i_value);
            Assert.AreEqual<double>(1.1, tc1.d_value);
            Assert.AreEqual<string>("one", tc1.s_value);

            Assert.AreEqual<int>(2, tc2.i_value);
            Assert.AreEqual<double>(2.2, tc2.d_value);
            Assert.AreEqual<string>("two", tc2.s_value);
        }

        [TestMethod]
        public void TestClass_IEquatable()
        {
            TestClass tc = new TestClass() { i_value = 1, d_value = 1.1, s_value = "one" };
            Assert.AreNotSame(tc, tc1);

            Assert.IsTrue(tc.Equals(tc1));
            Assert.IsFalse(tc.Equals(tc2));

            tc.i_value = tc2.i_value;
            Assert.IsFalse(tc.Equals(tc1));

            tc.i_value = tc1.i_value;
            tc.d_value = tc2.d_value;
            Assert.IsFalse(tc.Equals(tc1));

            tc.d_value = tc1.d_value;
            tc.s_value = tc2.s_value;
            Assert.IsFalse(tc.Equals(tc1));
        }

        [TestMethod]
        public void TestClass_ICloneable()
        {
        }

        [TestMethod]
        public void TestClass_SerializationTests()
        {

        }
    }
}
