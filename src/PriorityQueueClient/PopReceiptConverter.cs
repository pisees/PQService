// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.QueueClient
{
    using System;
    using System.Globalization;
    using System.ComponentModel;
    using System.Security.Permissions;
    using Common;

    /// <summary>
    /// Converts a PopReceipt to a different type.
    /// </summary>
    [HostProtection(SharedState = true)]
    internal sealed class PopReceiptConverter : TypeConverter
    {
        /// <summary>
        /// Gets a value indicating whether this converter can convert an object
        /// in the give source type to a SHA256 using the context type.
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            // Can only convert from a string type.
            if (typeof(string) == sourceType)
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Gets a value indicating whether this converter can convert an object
        /// to the given destination type using the context.
        /// </summary>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (typeof(string) == destinationType)
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Converts the given object to a SHA256.
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                string text = ((string)value).Trim();
                return new PopReceipt(text);
            }

            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Converts the given object to another type. The most common types to convert
        /// are to and from a string object. The default implementation will make a call
        /// to ToString on the object if the object is value and if the destination type
        /// is string. If this cannot convert to the destination type, this will throw
        /// a NotSupportedException.
        /// </summary>
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            Guard.ArgumentNotNull(destinationType, nameof(destinationType));

            if ((typeof(string) == destinationType) && (value is PopReceipt))
            {
                return value.ToString();
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
