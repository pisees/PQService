// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.Common
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    
    /// <summary>
    /// Implements the common guard methods.
    /// </summary>
    public static class Guard
    {
        /// <summary>
        /// Checks an argument to ensure its value is expected value
        /// </summary>
        /// <typeparam name="T">The type of the argument.</typeparam>
        /// <param name="argumentValue">The value of the argument.</param>
        /// <param name="expectedValue">The expected value of the argument.</param>
        /// <param name="argumentName">The name of the argument for diagnostic purposes.</param>
        public static void ArgumentIsEqual<T>([ValidatedNotNull] T argumentValue, [ValidatedNotNull] T expectedValue, string argumentName)
        {
            if (Comparer<T>.Default.Compare(argumentValue, expectedValue) != 0)
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Resources.InvalidArgumentValue, argumentName, expectedValue));
            }
        }

        /// <summary>
        /// Checks a string argument to ensure it isn't null or empty.
        /// </summary>
        /// <param name="argumentValue">The argument value to check.</param>
        /// <param name="argumentName">The name of the argument.</param>    
        public static void ArgumentNotNullOrEmptyString([ValidatedNotNull] string argumentValue, string argumentName)
        {
            ArgumentNotNull(argumentValue, argumentName);

            if (argumentValue.Length == 0)
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Resources.StringCannotBeEmpty, argumentName));
            }
        }

        /// <summary>
        /// Checks a string argument to ensure it isn't null or empty.
        /// </summary>
        /// <param name="argumentValue">The argument value to check.</param>
        /// <param name="argumentName">The name of the argument.</param>    
        public static void ArgumentNotNullOrWhitespace([ValidatedNotNull] string argumentValue, string argumentName)
        {
            ArgumentNotNull(argumentValue, argumentName);

            if (string.IsNullOrWhiteSpace(argumentValue))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Resources.StringCannotBeEmpty, argumentName));
            }
        }

        /// <summary>
        /// Checks an argument to ensure it isn't null.
        /// </summary>
        /// <param name="argumentValue">The argument value to check.</param>
        /// <param name="argumentName">The name of the argument.</param>
        public static void ArgumentNotNull<T>([ValidatedNotNull] T argumentValue, string argumentName)
        {
            if (argumentValue == null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        /// <summary>
        /// Checks an argument to ensure that its value is not the default value for its type.
        /// </summary>
        /// <typeparam name="T">The type of the argument.</typeparam>
        /// <param name="argumentValue">The value of the argument.</param>
        /// <param name="argumentName">The name of the argument for diagnostic purposes.</param>
        public static void ArgumentNotDefaultValue<T>(T argumentValue, string argumentName)
        {
            if (IsDefaultValue(argumentValue))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Resources.ArgumentCannotBeDefault, argumentName));
            }
        }

        /// <summary>
        /// Checks that string argument value matches the given regex.
        /// </summary>
        /// <param name="argumentValue">The value of the argument.</param>
        /// <param name="pattern">The regex pattern match.</param>
        /// <param name="argumentName">The name of the argument for diagnostic purposes.</param>
        public static void ArgumentNotNullAndMatchRegex([ValidatedNotNull] string argumentValue, string pattern, string argumentName)
        {
            ArgumentNotNull(argumentValue, argumentName);

            if (!Regex.IsMatch(argumentValue, pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(10)))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Resources.StringMustMatchRegex, argumentName, pattern));
            }
        }

        /// <summary>
        /// Checks that all values of the specified argument satisfy a given condition.
        /// </summary>
        /// <typeparam name="T">The type of the argument.</typeparam>
        /// <param name="argumentValues">The values of the argument.</param>
        /// <param name="predicate">The condition to satisfy.</param>
        /// <param name="argumentName">The name of the argument for diagnostic purposes.</param>
        public static void ArgumentsSatisfyCondition<T>(IEnumerable<T> argumentValues, Func<T, bool> predicate, string argumentName)
        {
            if (argumentValues != null && !argumentValues.All(predicate))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Resources.ArgumentsConditionNotSatisfied, argumentName));
            }
        }

        /// <summary>
        /// Checks whether or not the specified collection is empty.
        /// </summary>
        /// <typeparam name="T">The type of the argument.</typeparam>
        /// <param name="argumentValues">The values of the argument.</param>
        /// <param name="argumentName">The name of the argument for diagnostic purposes.</param>
        public static void ArgumentCollectionNotEmpty<T>(IEnumerable<T> argumentValues, string argumentName)
        {
            if (argumentValues == null || !argumentValues.Any())
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Resources.ArgumentCollectionCannotBeEmpty, argumentName));
            }
        }

        /// <summary>
        /// Checks an argument of type <see cref="System.Int32"/> to ensure that its value is not zero or negative.
        /// </summary>
        /// <param name="argumentValue">The <see cref="System.Int32"/> value of the argument.</param>
        /// <param name="argumentName">The name of the argument for diagnostic purposes.</param>
        public static void ArgumentNotZeroOrNegativeValue(int argumentValue, string argumentName)
        {
            if (argumentValue <= 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, argumentValue, String.Format(CultureInfo.CurrentCulture, Resources.ArgumentCannotBeZeroOrNegative, argumentName));
            }
        }

        /// <summary>
        /// Checks an argument of type <see cref="System.Int32"/> to ensure that its value is not negative.
        /// </summary>
        /// <param name="argumentValue">The <see cref="System.Int32"/> value of the argument.</param>
        /// <param name="argumentName">The name of the argument for diagnostic purposes.</param>
        public static void ArgumentNotNegativeValue(int argumentValue, string argumentName)
        {
            if (argumentValue < 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, argumentValue, String.Format(CultureInfo.CurrentCulture, Resources.ArgumentCannotBeNegative, argumentName));
            }
        }

        /// <summary>
        /// Checks an argument of type <see cref="System.Int64"/> to ensure that its value is not zero or negative.
        /// </summary>
        /// <param name="argumentValue">The <see cref="System.Int64"/> value of the argument.</param>
        /// <param name="argumentName">The name of the argument for diagnostic purposes.</param>
        public static void ArgumentNotZeroOrNegativeValue(long argumentValue, string argumentName)
        {
            if (argumentValue <= 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, argumentValue, String.Format(CultureInfo.CurrentCulture, Resources.ArgumentCannotBeZeroOrNegative, argumentName));
            }
        }

        /// <summary>
        /// Checks an argument of type <see cref="System.Int64"/> to ensure that its value is not negative.
        /// </summary>
        /// <param name="argumentValue">The <see cref="System.Int64"/> value of the argument.</param>
        /// <param name="argumentName">The name of the argument for diagnostic purposes.</param>
        public static void ArgumentNotNegativeValue(long argumentValue, string argumentName)
        {
            if (argumentValue < 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, argumentValue, String.Format(CultureInfo.CurrentCulture, Resources.ArgumentCannotBeNegative, argumentName));
            }
        }

        /// <summary>
        /// Checks an argument of type <see cref="System.Decimal"/> to ensure that its value is not zero or negative.
        /// </summary>
        /// <param name="argumentValue">The <see cref="System.Decimal"/> value of the argument.</param>
        /// <param name="argumentName">The name of the argument for diagnostic purposes.</param>
        public static void ArgumentNotZeroOrNegativeValue(decimal argumentValue, string argumentName)
        {
            if (argumentValue <= 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, argumentValue, String.Format(CultureInfo.CurrentCulture, Resources.ArgumentCannotBeZeroOrNegative, argumentName));
            }
        }

        /// <summary>
        /// Checks an argument of type <see cref="System.Decimal"/> to ensure that its value is not negative.
        /// </summary>
        /// <param name="argumentValue">The <see cref="System.Decimal"/> value of the argument.</param>
        /// <param name="argumentName">The name of the argument for diagnostic purposes.</param>
        public static void ArgumentNotNegativeValue(decimal argumentValue, string argumentName)
        {
            if (argumentValue < 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, argumentValue, String.Format(CultureInfo.CurrentCulture, Resources.ArgumentCannotBeNegative, argumentName));
            }
        }

        /// <summary>
        /// Checks an argument of type <see cref="System.Double"/> to ensure that its value is not zero or negative.
        /// </summary>
        /// <param name="argumentValue">The <see cref="System.Double"/> value of the argument.</param>
        /// <param name="argumentName">The name of the argument for diagnostic purposes.</param>
        public static void ArgumentNotZeroOrNegativeValue(double argumentValue, string argumentName)
        {
            if (argumentValue <= 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, argumentValue, String.Format(CultureInfo.CurrentCulture, Resources.ArgumentCannotBeZeroOrNegative, argumentName));
            }
        }

        /// <summary>
        /// Checks an argument of type <see cref="System.Double"/> to ensure that its value is not negative.
        /// </summary>
        /// <param name="argumentValue">The <see cref="System.Double"/> value of the argument.</param>
        /// <param name="argumentName">The name of the argument for diagnostic purposes.</param>
        public static void ArgumentNotNegativeValue(double argumentValue, string argumentName)
        {
            if (argumentValue < 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, argumentValue, String.Format(CultureInfo.CurrentCulture, Resources.ArgumentCannotBeNegative, argumentName));
            }
        }

        /// <summary>
        /// Checks an argument of type <see cref="System.Single"/> to ensure that its value is not zero or negative.
        /// </summary>
        /// <param name="argumentValue">The <see cref="System.Single"/> value of the argument.</param>
        /// <param name="argumentName">The name of the argument for diagnostic purposes.</param>
        public static void ArgumentNotZeroOrNegativeValue(float argumentValue, string argumentName)
        {
            if (argumentValue <= 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, argumentValue, String.Format(CultureInfo.CurrentCulture, Resources.ArgumentCannotBeZeroOrNegative, argumentName));
            }
        }

        /// <summary>
        /// Checks an argument of type <see cref="System.Single"/> to ensure that its value is not negative.
        /// </summary>
        /// <param name="argumentValue">The <see cref="System.Single"/> value of the argument.</param>
        /// <param name="argumentName">The name of the argument for diagnostic purposes.</param>
        public static void ArgumentNotNegativeValue(float argumentValue, string argumentName)
        {
            if (argumentValue < 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, argumentValue, String.Format(CultureInfo.CurrentCulture, Resources.ArgumentCannotBeNegative, argumentName));
            }
        }

        /// <summary>
        /// Checks an argument of type <see cref="System.TimeSpan"/> to ensure that its value is not zero or negative.
        /// </summary>
        /// <param name="argumentValue">The <see cref="System.TimeSpan"/> value of the argument.</param>
        /// <param name="argumentName">The name of the argument for diagnostic purposes.</param>
        public static void ArgumentNotZeroOrNegativeValue(TimeSpan argumentValue, string argumentName)
        {
            if (argumentValue <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(argumentName, argumentValue, String.Format(CultureInfo.CurrentCulture, Resources.ArgumentCannotBeZeroOrNegative, argumentName));
            }
        }

        /// <summary>
        /// Checks an argument of type <see cref="System.TimeSpan"/> to ensure that its value is not negative.
        /// </summary>
        /// <param name="argumentValue">The <see cref="System.TimeSpan"/> value of the argument.</param>
        /// <param name="argumentName">The name of the argument for diagnostic purposes.</param>
        public static void ArgumentNotNegativeValue(TimeSpan argumentValue, string argumentName)
        {
            if (argumentValue < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(argumentName, argumentValue, String.Format(CultureInfo.CurrentCulture, Resources.ArgumentCannotBeNegative, argumentName));
            }
        }

        /// <summary>
        /// Checks if the supplied argument falls into the given range of values.
        /// </summary>
        /// <typeparam name="T">The type of the argument.</typeparam>
        /// <param name="argumentValue">The value of the argument.</param>
        /// <param name="minValue">The minimum allowed value of the argument.</param>
        /// <param name="maxValue">The maximum allowed value of the argument.</param>
        /// <param name="argumentName">The name of the argument for diagnostic purposes.</param>
        public static void ArgumentInRange<T>(T argumentValue, T minValue, T maxValue, string argumentName) where T : IComparable<T>
        {
            if (Comparer<T>.Default.Compare(argumentValue, minValue) < 0 || Comparer<T>.Default.Compare(argumentValue, maxValue) > 0)
            {
                throw new ArgumentOutOfRangeException(argumentName, argumentValue, String.Format(CultureInfo.CurrentCulture, Resources.ArgumentCannotBeOutOfRange, argumentName, minValue, maxValue));
            }
        }

        /// <summary>
        /// Checks if the supplied argument present in the collection of possible values.
        /// </summary>
        /// <remarks>
        /// Comparison is case sensitive
        /// </remarks>
        /// <typeparam name="T">The type of the argument.</typeparam>
        /// <param name="argumentValue">The value of the argument.</param>
        /// <param name="collection">Collection of possible values</param>
        /// <param name="argumentName">The name of the argument for diagnostic purposes.</param>
        public static void ArgumentInCollection<T>(T argumentValue, IEnumerable<T> collection, string argumentName) where T : IComparable<T>
        {
            Guard.ArgumentNotNull(collection, "collection");
            if (!collection.Contains(argumentValue))
            {
                throw new ArgumentOutOfRangeException(argumentName, argumentValue, String.Format(CultureInfo.CurrentCulture, Resources.ArgumentNotInCollection, argumentName, string.Join(",", collection)));
            }
        }

        /// <summary>
        /// Checks an argument to ensure that its value doesn't exceed the specified ceiling baseline.
        /// </summary>
        /// <param name="argumentValue">The <see cref="System.Double"/> value of the argument.</param>
        /// <param name="ceilingValue">The <see cref="System.Double"/> ceiling value of the argument.</param>
        /// <param name="argumentName">The name of the argument for diagnostic purposes.</param>
        public static void ArgumentNotGreaterThan(double argumentValue, double ceilingValue, string argumentName)
        {
            if (argumentValue > ceilingValue)
            {
                throw new ArgumentOutOfRangeException(argumentName, argumentValue, String.Format(CultureInfo.CurrentCulture, Resources.ArgumentCannotBeGreaterThanBaseline, argumentName, ceilingValue));
            }
        }

        /// <summary>
        /// Checks an argument to ensure that its value doesn't exceed the specified ceiling baseline.
        /// </summary>
        /// <param name="array">The <see cref="System.Array"/>Array to evaluate.</param>
        /// <param name="length">The <see cref="System.Int32"/>Desired length.</param>
        /// <param name="argumentName">The name of the argument for diagnostic purposes.</param>
        public static void ArrayLengthEquals(Array array, int length, string argumentName)
        {
            Guard.ArgumentNotNull(array, nameof(array));

            if (array.Length != length)
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Resources.ArrayLengthNotEqual, array.Length, length), argumentName);
            }
        }

        /// <summary>
        /// Checks an enum instance to ensure that its value is defined by the specified enum type.
        /// </summary>
        /// <typeparam name="T">The type of the enum.</typeparam>
        /// <param name="enumValue">The enum value to check.</param>
        /// <param name="argumentName">The name of the argument holding the value.</param>
        /// <remarks>
        /// This method does not currently support Flags enums.
        /// The constraint on the method should be updated to "enum" once the C# compiler supports it.
        /// </remarks>
        public static void EnumValueIsDefined<T>(T enumValue, string argumentName) where T : struct
        {
            if (!typeof(T).IsEnumDefined(enumValue))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Resources.InvalidEnumValue, argumentName, typeof(T)));
            }
        }

        /// <summary>
        /// Verifies that an argument type is assignable from the provided type (meaning
        /// interfaces are implemented, or classes exist in the base class hierarchy).
        /// </summary>
        /// <param name="assignee">The argument type.</param>
        /// <param name="providedType">The type it must be assignable from.</param>
        /// <param name="argumentName">The argument name.</param>
        public static void TypeIsAssignableFromType(Type assignee, Type providedType, string argumentName)
        {
            Guard.ArgumentNotNull(assignee, "assignee");
            Guard.ArgumentNotNull(providedType, "providedType");

            if (!providedType.IsAssignableFrom(assignee))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, Resources.TypeNotCompatible, assignee, providedType), argumentName);
            }
        }

        #region Private methods
        
        /// <summary>
        /// Determines whether the specified value is the default value for its type.
        /// </summary>
        /// <typeparam name="T">The type of the value to be checked.</typeparam>
        /// <param name="value">The value to be checked.</param>
        /// <returns><c>true</c> if the given value is the default value for its type.; otherwise, <c>false</c>.</returns>
        private static bool IsDefaultValue<T>(T value)
        {
            return Object.Equals(value, default(T));
        }

        #endregion
        
        #region Nested types
        
        /// <summary>
        /// This attribute class tells Code Analysis (FxCop) that a method validates that a parameter is not null
        /// </summary>
        internal sealed class ValidatedNotNullAttribute : Attribute
        {
        }
        
        #endregion
    }
}
