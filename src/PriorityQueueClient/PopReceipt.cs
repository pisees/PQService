// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#define CONTRACTS_FULL

using System;

using System.Globalization;
using System.ComponentModel;
using System.Runtime.InteropServices;
using QuickService.Common;

namespace QuickService.QueueClient
{
    /// <summary>
    /// PopReceipt value type.
    /// </summary>
    [Serializable]
    [TypeConverter(typeof(PopReceiptConverter))]
    [StructLayout(LayoutKind.Explicit)]
    public struct PopReceipt : IFormattable, IEquatable<PopReceipt>, IComparable<PopReceipt>
    {
        /// <summary>
        /// Gets the empty PopReceipt.
        /// </summary>
        public static readonly PopReceipt Empty = new PopReceipt(new byte[PopReceiptByteLength]);

        /// <summary>
        /// Byte length of a valid PopReceipt.
        /// </summary>
        private const int PopReceiptByteLength = 4 * sizeof(UInt64);

        /// <summary>
        /// String length of a valid PopReceipt.
        /// </summary>
        private const int PopReceiptStringLength = 16 + 32 + 16;     // Number of hex digits in a long + number of hex digits in a Guid + hex digits in a long (eTag).

        #region Private Fields

        /// <summary>
        /// PopReceipt is represented by 4 UInt64 values. This is the first.
        /// </summary>
        [FieldOffset(0)]
        private readonly UInt64 _a;

        /// <summary>
        /// PopReceipt is represented by 4 UInt64 values. This is the second.
        /// </summary>
        [FieldOffset(8)]
        private readonly UInt64 _b;

        /// <summary>
        /// PopReceipt is represented by 4 UInt64 values. This is the third.
        /// </summary>
        [FieldOffset(16)]
        private readonly UInt64 _c;

        /// <summary>
        /// PopReceipt is represented by 4 UInt64 values. This is the fourth.
        /// </summary>
        [FieldOffset(24)]
        private readonly UInt64 _d;

        #endregion

        #region Public Fields

        /// <summary>
        /// Unique identifier of the PopReceipt.
        /// </summary>
        [FieldOffset(0)]
        public readonly Guid Id;

        /// <summary>
        /// UInt64 value containing the partition of the PopReceipt.
        /// </summary>
        [FieldOffset(16)]
        public readonly long Partition;

        /// <summary>
        /// UInt64 tag value used to control concurrency.
        /// </summary>
        [FieldOffset(24)]
        public readonly UInt64 Tag;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a PopReceipt based on a byte array.
        /// </summary>
        /// <param name="b">Byte array representation of a PopReceipt.</param>
        internal PopReceipt(byte[] b)
            : this()
        {
            //Contract.Requires<ArgumentNullException>(null == b, "Argument b is null.");
            //Contract.Requires<ArgumentException>(b.Length == PopReceiptByteLength, "Byte array does not contain the correct number of bytes for a serialized PopReceipt.");
            Guard.ArgumentNotNull(b, nameof(b));
            Guard.ArrayLengthEquals(b, PopReceiptByteLength, nameof(b));

            // Read in the values.
            _a = BitConverter.ToUInt64(b, 0);
            _b = BitConverter.ToUInt64(b, 8);
            _c = BitConverter.ToUInt64(b, 16);
            _d = BitConverter.ToUInt64(b, 24);
        }

        /// <summary>
        /// Creates a PopReceipt based on a string.
        /// </summary>
        /// <param name="s">String representation of a PopReceipt.</param>
        internal PopReceipt(string s)
            : this()
        {
            Guard.ArgumentNotNullOrWhitespace(s, nameof(s));
            Guard.ArgumentIsEqual(s.Length, PopReceiptStringLength, nameof(s));

            _a = UInt64.Parse(s.Substring(0, 16), NumberStyles.HexNumber);
            _b = UInt64.Parse(s.Substring(16, 16), NumberStyles.HexNumber);
            _c = UInt64.Parse(s.Substring(32, 16), NumberStyles.HexNumber);
            _d = UInt64.Parse(s.Substring(48, 16), NumberStyles.HexNumber);
        }

        /// <summary>
        /// Creates a PopReceipt based on the constituent fields.
        /// </summary>
        /// <param name="partition">Partition number.</param>
        /// <param name="id">Unique identifier for this PopReceipt.</param>
        /// <param name="tag">Tag for concurrency control. Default is zero.</param>
        public PopReceipt(long partition, Guid id, UInt64 tag = 0L)
            : this()
        {
            Guard.ArgumentInRange(partition, 0L, long.MaxValue, nameof(partition));

            Partition = partition;
            Tag = tag;
            Id = id;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a new PopReceipt in the specified partition.
        /// </summary>
        /// <param name="partition">Long integer representing the partition.</param>
        /// <returns>PopReceipt instance.</returns>
        public static PopReceipt NewPopReceipt(long partition)
        {
            Guard.ArgumentInRange(partition, 0L, long.MaxValue, nameof(partition));
            return new PopReceipt(partition, Guid.NewGuid(), 0L);
        }

        /// <summary>
        /// Gets the PopReceipt as an array of bytes.
        /// </summary>
        /// <returns>byte[].</returns>
        public byte[] ToByteArray()
        {
            byte[] b = new byte[PopReceiptByteLength];

            b[0] = (byte)_a;
            b[1] = (byte)(_a >> 8);
            b[2] = (byte)(_a >> 16);
            b[3] = (byte)(_a >> 24);
            b[4] = (byte)(_a >> 32);
            b[5] = (byte)(_a >> 40);
            b[6] = (byte)(_a >> 48);
            b[7] = (byte)(_a >> 56);
            b[8] = (byte)_b;
            b[9] = (byte)(_b >> 8);
            b[10] = (byte)(_b >> 16);
            b[11] = (byte)(_b >> 24);
            b[12] = (byte)(_b >> 32);
            b[13] = (byte)(_b >> 40);
            b[14] = (byte)(_b >> 48);
            b[15] = (byte)(_b >> 56);
            b[16] = (byte)_c;
            b[17] = (byte)(_c >> 8);
            b[18] = (byte)(_c >> 16);
            b[19] = (byte)(_c >> 24);
            b[20] = (byte)(_c >> 32);
            b[21] = (byte)(_c >> 40);
            b[22] = (byte)(_c >> 48);
            b[23] = (byte)(_c >> 56);
            b[24] = (byte)_d;
            b[25] = (byte)(_d >> 8);
            b[26] = (byte)(_d >> 16);
            b[27] = (byte)(_d >> 24);
            b[28] = (byte)(_d >> 32);
            b[29] = (byte)(_d >> 40);
            b[30] = (byte)(_d >> 48);
            b[31] = (byte)(_d >> 56);

            return b;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object
        /// </summary>
        /// <param name="obj">Instance to compare.</param>
        /// <returns>True if the objects are considered equal; otherwise, false. If both objA and objB are null, the method returns true.</returns>
        public override bool Equals(object obj)
        {
            return CompareTo(obj) == 0;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object
        /// </summary>
        /// <param name="other">Instance to compare.</param>
        /// <returns>True if the objects are considered equal; otherwise, false. If both objA and objB are null, the method returns true.</returns>
        bool IEquatable<PopReceipt>.Equals(PopReceipt other)
        {
            return CompareTo(other) == 0;
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return _a.GetHashCode() ^ _b.GetHashCode() ^ _c.GetHashCode() ^ _d.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return ToString("g", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <param name="format">Format specifier.</param>
        /// <returns>A string that represents the current object.</returns>
        /// <remarks>Supports two format specifiers, "G" for upper case letters and "g" for lower case letters.</remarks>
        public string ToString(string format)
        {
            return ToString(format, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <param name="format">Format specifier.</param>
        /// <param name="formatProvider">Format provider instance.</param>
        /// <returns>A string that represents the current object.</returns>
        /// <remarks>Supports two format specifiers, "G" for upper case letters and "g" for lower case letters.</remarks>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrWhiteSpace(format)) format = "g";
            if (null == formatProvider) formatProvider = CultureInfo.InvariantCulture;

            if ("G" == format)
            {
                return string.Format("{0:X16}{1:X16}{2:X16}{3:X16}", _a, _b, _c, _d);
            }
            else if ("g" == format)
            {
                return string.Format("{0:x16}{1:x16}{2:x16}{3:x16}", _a, _b, _c, _d);
            }

            throw new FormatException($"The {format} format specifier is not supported.");
        }

        /// <summary>
        /// Compares an object this value.
        /// </summary>
        /// <param name="other">Object to compare.</param>
        /// <returns>1, -1 or 0.</returns>
        public int CompareTo(object other)
        {
            if (null == other) return 1;

            PopReceipt pr;
            if (other is string)
            {
                pr = new PopReceipt((other.ToString()));
            }
            else if (other is byte[])
            {
                pr = new PopReceipt((byte[]) other);
            }
            else if (other is PopReceipt)
            {
                pr = (PopReceipt) other;
            }
            else
            {
                throw new ArgumentException("Argument is not a PopReceipt.");
            }

            return CompareTo(pr);
        }

        /// <summary>
        /// Compares a PopReceipt this value.
        /// </summary>
        /// <param name="other">PopReceipt to compare.</param>
        /// <returns>1, -1 or 0.</returns>
        public int CompareTo(PopReceipt other)
        {
            if (_a != other._a)
                return (_a < other._a) ? -1 : 1;
            if (_b != other._b)
                return (_b < other._b) ? -1 : 1;
            if (_c != other._c)
                return (_c < other._c) ? -1 : 1;
            if (_d != other._d)
                return (_d < other._d) ? -1 : 1;

            return 0;
        }

        /// <summary>
        /// Not equals operator.
        /// </summary>
        /// <returns>False if equal, otherwise True.</returns>
        public static bool operator !=(PopReceipt p1, PopReceipt p2)
        {
            return !(p1.Equals(p2));
        }

        /// <summary>
        /// Not equals operator 
        /// </summary>
        /// <returns>False if equal, otherwise True.</returns>
        public static bool operator !=(string s1, PopReceipt s2)
        {
            return !(s1.Equals(s2.ToString()));
        }

        /// <summary>
        /// Not equals operator 
        /// </summary>
        /// <returns>False if equal, otherwise True.</returns>
        public static bool operator !=(PopReceipt s1, string s2)
        {
            return !(s2.Equals(s1.ToString()));
        }

        /// <summary>
        /// Equals operator.
        /// </summary>
        /// <returns>True if equal, otherwise False.</returns>
        public static bool operator ==(PopReceipt s1, PopReceipt s2)
        {
            return (s1.Equals(s2));
        }

        /// <summary>
        /// Equals operator.
        /// </summary>
        /// <returns>True if equal, otherwise False.</returns>
        public static bool operator ==(string s1, PopReceipt s2)
        {
            return (s1.Equals(s2.ToString()));
        }

        /// <summary>
        /// Equals operator.
        /// </summary>
        /// <returns>True if equal, otherwise False.</returns>
        public static bool operator ==(PopReceipt s1, string s2)
        {
            return (s2.Equals(s1.ToString()));
        }

        #endregion
    }
}
