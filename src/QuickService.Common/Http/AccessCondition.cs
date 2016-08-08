// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.Common.Rest
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Http;
    using System.Net.Http.Headers;

    /// <summary>
    /// Represents a set of access conditions to be used for operations against REST services.
    /// </summary>
    public sealed class AccessCondition
    {
        #region Http Header Related Constants

        /// <summary>
        /// The ETag wildcard value (without double quotes)
        /// </summary>
        public const string ETagWildcard = "*";

        /// <summary>
        /// If-Match header name
        /// </summary>
        public const string IfMatchHeader = "If-Match";

        /// <summary>
        /// If-None-Match header name
        /// </summary>
        public const string IfNoneMatchHeader = "If-None-Match";

        /// <summary>
        /// If-Modified-Since header name
        /// </summary>
        public const string IfModifiedSinceHeader = "If-Modified-Since";

        /// <summary>
        /// If-Unmodified-Since header name
        /// </summary>
        public const string IfUnmodifiedSinceHeader = "If-Unmodified-Since";

        #endregion

        /// <summary>
        /// Time for IfModifiedSince.
        /// </summary>
        private DateTimeOffset? ifModifiedSinceDateTime;

        /// <summary>
        /// Time for IfUnmodifiedSince.
        /// </summary>
        private DateTimeOffset? ifNotModifiedSinceDateTime;

        /// <summary>
        /// Gets or sets an ETag value for a condition specifying that the given ETag must match the ETag of the specified resource.
        /// </summary>
        /// <value>A string containing an ETag value, or <c>"*"</c> to match any ETag. If <c>null</c>, no condition exists.</value>
        public string IfMatchETag
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets an ETag value for a condition specifying that the given ETag must not match the ETag of the specified resource.
        /// </summary>
        /// <value>A string containing an ETag value, or <c>"*"</c> to match any ETag. If <c>null</c>, no condition exists.</value>
        public string IfNoneMatchETag
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a <see cref="DateTimeOffset"/> value for a condition specifying a time since which a resource has been modified.
        /// </summary>
        /// <value>A <see cref="DateTimeOffset"/> value specified in UTC, or <c>null</c> if no condition exists.</value>
        public DateTimeOffset? IfModifiedSinceTime
        {
            get
            {
                return this.ifModifiedSinceDateTime;
            }

            set
            {
                this.ifModifiedSinceDateTime = value.HasValue ? value.Value.ToUniversalTime() : value;
            }
        }

        /// <summary>
        /// Gets or sets a <see cref="DateTimeOffset"/> value for a condition specifying a time since which a resource has not been modified.
        /// </summary>
        /// <value>A <see cref="DateTimeOffset"/> value specified in UTC, or <c>null</c> if no condition exists.</value>
        public DateTimeOffset? IfNotModifiedSinceTime
        {
            get
            {
                return this.ifNotModifiedSinceDateTime;
            }

            set
            {
                this.ifNotModifiedSinceDateTime = value.HasValue ? value.Value.ToUniversalTime() : value;
            }
        }

        /// <summary>
        /// Determines whether the access condition is one of the four conditional headers.
        /// </summary>
        /// <value><c>true</c> if the access condition is a conditional header; otherwise, <c>false</c>.</value>
        internal bool IsConditional
        {
            get
            {
                return !string.IsNullOrEmpty(this.IfMatchETag) ||
                    !string.IsNullOrEmpty(this.IfNoneMatchETag) ||
                    this.IfModifiedSinceTime.HasValue ||
                    this.IfNotModifiedSinceTime.HasValue;
            }
        }

        /// <summary>
        /// Constructs an empty access condition.
        /// </summary>
        /// <returns>An empty <see cref="AccessCondition"/> object.</returns>
        public static AccessCondition GenerateEmptyCondition()
        {
            return new AccessCondition();
        }

        /// <summary>
        /// Constructs an access condition such that an operation will be performed only if the resource does not exist.
        /// </summary>
        /// <returns>An <see cref="AccessCondition"/> object that represents a condition where a resource does not exist.</returns>
        /// <remarks>Setting this access condition modifies the request to include the HTTP <i>If-None-Match</i> conditional header.</remarks>
        public static AccessCondition GenerateIfNotExistsCondition()
        {
            return new AccessCondition { IfNoneMatchETag = ETagWildcard };
        }

        /// <summary>
        /// Constructs an access condition such that an operation will be performed only if the resource exists
        /// <i>If-Match</i> conditional header will be set to <c>"*"</c>
        /// </summary>
        /// <returns>An <see cref="AccessCondition"/> object that represents a condition where a resource exists.</returns>
        /// <remarks>Setting this access condition modifies the request to include the HTTP <i>If-Match</i> conditional header.</remarks>
        public static AccessCondition GenerateIfExistsCondition()
        {
            return new AccessCondition { IfMatchETag = ETagWildcard };
        }

        /// <summary>
        /// Constructs an access condition such that an operation will be performed only if the resource's ETag value
        /// matches the specified ETag value.
        /// </summary>
        /// <param name="etag">The ETag value to check against the resource's ETag.</param>
        /// <returns>An <see cref="AccessCondition"/> object that represents the If-Match condition.</returns>
        public static AccessCondition GenerateIfMatchCondition(string etag)
        {
            return new AccessCondition { IfMatchETag = etag };
        }

        /// <summary>
        /// Constructs an access condition such that an operation will be performed only if the resource has been
        /// modified since the specified time.
        /// </summary>
        /// <param name="modifiedTime">A <see cref="DateTimeOffset"/> value specifying the time since which the resource must have been modified.</param>
        /// <returns>An <see cref="AccessCondition"/> object that represents the If-Modified-Since condition.</returns>
        public static AccessCondition GenerateIfModifiedSinceCondition(DateTimeOffset modifiedTime)
        {
            return new AccessCondition { IfModifiedSinceTime = modifiedTime };
        }

        /// <summary>
        /// Constructs an access condition such that an operation will be performed only if the resource's ETag value
        /// does not match the specified ETag value.
        /// </summary>
        /// <param name="etag">The ETag value to check against the resource's ETag, or <c>"*"</c> to require that the resource does not exist.</param>
        /// <returns>An <see cref="AccessCondition"/> object that represents the If-None-Match condition.</returns>
        /// <remarks>
        /// If <c>"*"</c> is specified for the <paramref name="etag"/> parameter, then this condition requires that the resource does not exist.
        /// </remarks>        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "etag", Justification = "Reviewed: etag can be used for identifier names.")]
        public static AccessCondition GenerateIfNoneMatchCondition(string etag)
        {
            return new AccessCondition { IfNoneMatchETag = etag };
        }

        /// <summary>
        /// Constructs an access condition such that an operation will be performed only if the resource has not been
        /// modified since the specified time.
        /// </summary>
        /// <param name="modifiedTime">A <see cref="DateTimeOffset"/> value specifying the time since which the resource must not have been modified.</param>
        /// <returns>An <see cref="AccessCondition"/> object that represents the If-Unmodified-Since condition.</returns>
        public static AccessCondition GenerateIfNotModifiedSinceCondition(DateTimeOffset modifiedTime)
        {
            return new AccessCondition { IfNotModifiedSinceTime = modifiedTime };
        }

        /// <summary>
        /// Constructs an access condition from specified HttpRequestHeaders instance
        /// </summary>
        /// <param name="headers">HttpRequestHeaders instance</param>
        /// <returns>An <see cref="AccessCondition"/> object</returns>
        public static AccessCondition GenerateFromRequestHeaders(HttpRequestHeaders headers)
        {
            Guard.ArgumentNotNull(headers, nameof(headers));
            return new AccessCondition
            {
                IfMatchETag = headers.IfMatch?.ToString(),
                IfNoneMatchETag = headers.IfNoneMatch?.ToString(),
                IfModifiedSinceTime = headers.IfModifiedSince,
                IfNotModifiedSinceTime = headers.IfUnmodifiedSince
            };
        }
    }

    /// <summary>
    /// Extension methods for the AccessCondition class.
    /// </summary>
    public static class AccessConditionExtensionMethods
    {
        /// <summary>
        /// Applies the condition in case it is not null to the Http request.
        /// </summary>
        /// <param name="accessCondition">Access condition to be added to the request.</param>
        /// <param name="requestHeaders">The request headers to be modified.</param>
        public static void ApplyAccessCondition(this AccessCondition accessCondition, HttpRequestHeaders requestHeaders)
        {
            if (accessCondition != null)
            {
                if (!string.IsNullOrEmpty(accessCondition.IfMatchETag))
                {
                    if (accessCondition.IfMatchETag.Equals(AccessCondition.ETagWildcard, StringComparison.OrdinalIgnoreCase))
                    {
                        requestHeaders.IfMatch.Add(EntityTagHeaderValue.Any);
                    }
                    else
                    {
                        requestHeaders.IfMatch.Add(EntityTagHeaderValue.Parse(accessCondition.IfMatchETag));
                    }
                }

                if (!string.IsNullOrEmpty(accessCondition.IfNoneMatchETag))
                {
                    if (accessCondition.IfNoneMatchETag.Equals(AccessCondition.ETagWildcard, StringComparison.OrdinalIgnoreCase))
                    {
                        requestHeaders.IfNoneMatch.Add(EntityTagHeaderValue.Any);
                    }
                    else
                    {
                        requestHeaders.IfNoneMatch.Add(EntityTagHeaderValue.Parse(accessCondition.IfNoneMatchETag));
                    }
                }

                requestHeaders.IfModifiedSince = accessCondition.IfModifiedSinceTime;
                requestHeaders.IfUnmodifiedSince = accessCondition.IfNotModifiedSinceTime;
            }
        }
    }
}
