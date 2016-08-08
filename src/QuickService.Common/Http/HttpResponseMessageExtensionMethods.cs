// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.Common.Rest
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Web.Http;

    /// <summary>
    /// Extension methods for HttpResponseMessage.
    /// </summary>
    public static class HttpResponseMessageExtensionMethods
    {
        /// <summary>
        /// Gets the value of the ETag header for the HTTP response if present, otherwise null.
        /// </summary>
        public static string GetETag(this HttpResponseMessage httpResponseMessage) => httpResponseMessage?.Headers?.ETag?.Tag;

        /// <summary>
        /// JSON deserialize HttpContent from HttpResponseMessage to type T 
        /// In case T is a ValueType exception will be thrown when there is no content
        /// In case T is a ReferenceType and there is no response or no content available null will be returned
        /// </summary>
        /// <typeparam name="T">Type of the result.</typeparam>
        /// <param name="httpResponseMessage">Instance of HttpResponseMessage.</param>
        /// <returns>Deserialized result.</returns>
        public static T GetResult<T>(this HttpResponseMessage httpResponseMessage)
        {
            // Is the type of T a value type?
            if (typeof(T).IsValueType)
            {
                Guard.ArgumentNotNull(httpResponseMessage, nameof(httpResponseMessage));
                Guard.ArgumentNotNull(httpResponseMessage.Content, nameof(httpResponseMessage.Content));
                if (httpResponseMessage.StatusCode == HttpStatusCode.NoContent)
                {
                    throw new InvalidOperationException($"Result value type {typeof(T)} can not be deserialized when HttpStatusCode is NoContent (204)");
                }
            }
            else if ((httpResponseMessage == null) || (httpResponseMessage.Content == null))
            {
                return default(T);
            }

            return httpResponseMessage.Content.ReadAsAsync<T>().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Throws an HttpResponseException which has the Response and StatusCode if the IsSuccessStatusCode() is false.
        /// Basically StatusCode is NOT in the following range [200-299]
        /// </summary>
        /// <remarks>
        /// The reason we need this method because HttpResponseMessage.EnsureSuccessStatusCode() throws
        /// HttpRequestException which has nothing in it (no StatusCode and no body)
        /// </remarks>
        public static void ThrowHttpResponseExceptionOnBadStatusCode(this HttpResponseMessage httpResponseMessage)
        {
            Guard.ArgumentNotNull(httpResponseMessage, nameof(httpResponseMessage));
            if (false == httpResponseMessage.IsSuccessStatusCode)
            {
                throw new HttpResponseException(httpResponseMessage);
            }
        }
    }
}
