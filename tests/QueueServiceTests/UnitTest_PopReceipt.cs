// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QueueServiceTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using QuickService.Common.Queue;
    using QuickService.QueueClient;
    using System;

    [TestClass]
    public class PopReceipt_UnitTests
    {
        const string sEmptyPopReceipt = "0000000000000000000000000000000000000000000000000000000000000000";
        const string sTestPopReceipt1 = "00000000000000ff00112233445566778899aabbccddeeff0000000000000000";
        const string sTestPopReceipt2 = "00000000000000fe00112233445566778899aabbccddeeff0000000000000000";
        const string sTestPopReceipt3 = "00000000000000ff00112233445566778899aabbccddeefe0000000000000000";
        const string sInvalidPopReceipt = "00000000000000ff0011223344G566778899aabbccddeefe0000000000000000";

        [TestMethod]
        public void PopReceipt_ValidateInterfaces()
        {
            // Create a PopReceipt instances.
            PopReceipt pr = new PopReceipt();
            PopReceipt pr1 = new PopReceipt(sTestPopReceipt1);
            PopReceipt emptyPR = new PopReceipt(sEmptyPopReceipt);

            // Check that is the empty PopReceipt.
            Assert.AreEqual<PopReceipt>(PopReceipt.Empty, pr);

            // Validate that the instance can be case to each of the interfaces.
            IFormattable iFormattable = pr as IFormattable;
            Assert.IsNotNull(iFormattable);

            IComparable<PopReceipt> iComparablePR = pr as IComparable<PopReceipt>;
            Assert.IsNotNull(iComparablePR);

            IEquatable<PopReceipt> iEquatablePR
                = pr as IEquatable<PopReceipt>;
            Assert.IsNotNull(iEquatablePR);

            // Use each interface's methods.
            string sPR = iFormattable.ToString();
            Assert.AreEqual<string>(sEmptyPopReceipt, sPR);

            Assert.AreEqual<int>(-1, iComparablePR.CompareTo(pr1));
            Assert.IsFalse(iEquatablePR.Equals(pr1));
            Assert.IsTrue(iEquatablePR.Equals(emptyPR));
        }

        [TestMethod]
        public void PopReceipt_ConstructorTests()
        {
            // Create default PopReceipt and ensure it is the empty one.
            PopReceipt pr0 = new PopReceipt();
            Assert.AreEqual(PopReceipt.Empty, pr0);
            Assert.AreEqual(sEmptyPopReceipt, pr0.ToString());

            // Create a PopReceipt based on a string and round trip it to ensure they are the same.
            PopReceipt testPR2 = new PopReceipt(sTestPopReceipt1);
            string sPR2 = testPR2.ToString("g");
            Assert.AreEqual(sTestPopReceipt1, sPR2);

            // Convert into a byte array.
            byte[] bPR2 = testPR2.ToByteArray();
            PopReceipt pr1 = new PopReceipt(bPR2);
            Assert.AreEqual(testPR2, pr1);
            Assert.AreEqual(sTestPopReceipt1, pr1.ToString());
        }

        [TestMethod]
        public void PopReceipt_StaicOperationTests()
        {
            PopReceipt pr1 = new PopReceipt(sTestPopReceipt1);
            PopReceipt pr1a = new PopReceipt(sTestPopReceipt1);
            PopReceipt pr2 = new PopReceipt(sTestPopReceipt2);
            PopReceipt pr3 = new PopReceipt(sTestPopReceipt3);

            Assert.IsTrue(pr1a == pr1);
            Assert.IsTrue(pr1 != pr2);
            Assert.IsFalse(pr1a == pr2);
            Assert.IsFalse(pr1a != pr1);

            Assert.IsTrue(sTestPopReceipt1 == pr1);
            Assert.IsTrue(sTestPopReceipt1 != pr2);
            Assert.IsFalse(sTestPopReceipt1 == pr2);
            Assert.IsFalse(sTestPopReceipt1 != pr1);

            Assert.IsTrue(pr1a == sTestPopReceipt1);
            Assert.IsTrue(pr1 != sTestPopReceipt2);
            Assert.IsFalse(pr1a == sTestPopReceipt2);
            Assert.IsFalse(pr1a != sTestPopReceipt1);
        }

        [TestMethod]
        public void PopReceipt_EqualityTests()
        {
            PopReceipt pr1 = new PopReceipt(sTestPopReceipt1);
            PopReceipt pr1a = new PopReceipt(sTestPopReceipt1);
            PopReceipt pr2 = new PopReceipt(sTestPopReceipt2);

            // Test Equals
            Assert.IsFalse(pr1.Equals(null));
            Assert.IsFalse(pr1.Equals(pr2));
            Assert.IsFalse(pr1.Equals(sTestPopReceipt2));
            Assert.IsFalse(pr1.Equals(PopReceipt.Empty));

            // Test CompareTo
            Assert.AreEqual(1, pr1.CompareTo((object) null));
            Assert.AreEqual(1, pr1.CompareTo(pr2));
            Assert.AreEqual(-1, pr2.CompareTo(pr1));
            Assert.AreEqual(0, pr1.CompareTo(pr1a));
        }

        [TestMethod]
        public void PopReceipt_NewPopReceiptTests()
        {
            const string spr1 = "0000000000000000000000000000000000000000000000000000000000000000";
            const string spr2 = "00000000000000fe000000000000000000000000000000000000000000000000";
            const string spr3 = "7fffffffffffffff000000000000000000000000000000000000000000000000";

            PopReceipt pr = PopReceipt.NewPopReceipt(0);
            Assert.AreEqual(0, pr.Partition);
            Assert.AreNotEqual(spr1, pr.ToString());
            Console.WriteLine(pr);

            pr = PopReceipt.NewPopReceipt(254);
            Assert.AreEqual(254, pr.Partition);
            Assert.AreNotEqual(spr2, pr.ToString());
            Console.WriteLine(pr);

            pr = PopReceipt.NewPopReceipt(long.MaxValue);
            Assert.AreEqual(long.MaxValue, pr.Partition);
            Assert.AreNotEqual(spr3, pr.ToString());
            Console.WriteLine(pr);
        }

        [TestMethod]
        public void PopReceipt_ToStringTests()
        {
            PopReceipt pr1 = new PopReceipt(sEmptyPopReceipt);
            PopReceipt pr2 = new PopReceipt(sTestPopReceipt1);

            Assert.AreEqual(sTestPopReceipt1, pr2.ToString(null));
            Assert.AreEqual(sTestPopReceipt1, pr2.ToString(null, null));
            Assert.AreEqual(sTestPopReceipt1.ToUpperInvariant(), pr2.ToString("G"));

            Console.WriteLine(pr2.ToString());
            Console.WriteLine(pr2.ToString("G"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void PopReceipt_ByteConstructorException()
        {
            byte[] b = new byte[15];
            PopReceipt pr = new PopReceipt(b);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void PopReceipt_StringConstructorException()
        {
            PopReceipt pr = new PopReceipt(sInvalidPopReceipt);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void PopReceipt_ToStringException()
        {
            PopReceipt pr = new PopReceipt(sTestPopReceipt1);
            pr.ToString("u");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void PopReceipt_NewPopoReceiptException()
        {
            PopReceipt.NewPopReceipt(-1);
        }
    }
}
