// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.Common
{
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Converts a TimeSpan to and from JSON for JSON.Net.
    /// </summary>
    public class JsonTimeSpanConverter : JsonConverter
    {
        /// <summary>
        /// Indicates if the passed type can be converted.
        /// </summary>
        /// <param name="objectType">Type of object to convert.</param>
        /// <returns>True if the object is a TimeSpan, otherwise false.</returns>
        public override bool CanConvert(Type objectType)
        {
            if (objectType == typeof(TimeSpan))
                return true;

            return false;
        }

        /// <summary>
        /// Reads the JSON and returns a TimeSpan.
        /// </summary>
        /// <param name="reader">JsonReader instance positioned to the parsed JSON object.</param>
        /// <param name="objectType">Type of object.</param>
        /// <param name="existingValue">Value of the current item.</param>
        /// <param name="serializer">JsonSerializer instance.</param>
        /// <returns>TimeSpan instance</returns>
        /// <exception cref="InvalidCastException">ObjectType is not a TimeSpan object.</exception>
        /// <exception cref="OverflowException">One of the values of the JSON representation is out of range or contains too many digits.</exception>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return TimeSpan.Parse(reader.Value.ToString());
        }

        /// <summary>
        /// Writes a TimeSpan instance to the JSON stream.
        /// </summary>
        /// <param name="writer">JsonWriter instance.</param>
        /// <param name="value">Value to write.</param>
        /// <param name="serializer">JsonSerializer instance.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
