// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.Common.ErrorHandling
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Net;
    using System.Net.Http;
    using System.Fabric;

    /// <summary>
    /// The error detection strategy for Rest APIs.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", 
        Justification = "Reviewed. Suppression is OK here.")]
    public sealed class RestApiErrorDetectionStrategy : ITransientErrorDetectionStrategy<HttpResponseMessage>
    {
        /// <summary>
        /// The shared instance of this strategy.
        /// </summary>
        public static readonly RestApiErrorDetectionStrategy Instance = new RestApiErrorDetectionStrategy();

        /// <summary>
        /// Indicates if the exception is a transient exception or not.
        /// </summary>
        /// <param name="ex">Exception to evaluate.</param>
        /// <returns>True if transient, otherwise false.</returns>
        public bool IsTransientException(Exception ex)
        {
            return (ex is HttpRequestException) || (ex is FabricTransientException );
        }

        /// <summary>
        /// Indicates if the exception is a failure exception or not.
        /// </summary>
        /// <param name="result">Exception to evaluate.</param>
        /// <returns>True if failure, otherwise false.</returns>
        public bool IsFailure(HttpResponseMessage result)
        {
            // Everything below 400 range is considered a success
            return (result == null) || (result.StatusCode >= HttpStatusCode.BadRequest);
        }

        /// <summary>
        /// Indicates if the result is a transient failure or not.
        /// </summary>
        /// <param name="result">Result to evaluate.</param>
        /// <returns>True if transient, otherwise false.</returns>
        public bool IsTransientFailure(HttpResponseMessage result)
        {
            // If the result is null, assume transient failure.
            if (result == null)
            {
                return true; // How exactly we got here?
            }

            switch (result.StatusCode)
            {
                // Retry should never be attempted for any of the 400-499, they will always fail again
                case HttpStatusCode.BadRequest:                     // 400
                case HttpStatusCode.Unauthorized:                   // 401
                case HttpStatusCode.PaymentRequired:                // 402
                case HttpStatusCode.Forbidden:                      // 403
                case HttpStatusCode.NotFound:                       // 404
                case HttpStatusCode.MethodNotAllowed:               // 405
                case HttpStatusCode.NotAcceptable:                  // 406
                case HttpStatusCode.ProxyAuthenticationRequired:    // 407
                case HttpStatusCode.RequestTimeout:                 // 408
                case HttpStatusCode.Conflict:                       // 409
                case HttpStatusCode.Gone:                           // 410
                case HttpStatusCode.LengthRequired:                 // 411
                case HttpStatusCode.PreconditionFailed:             // 412
                case HttpStatusCode.RequestEntityTooLarge:          // 413
                case HttpStatusCode.RequestUriTooLong:              // 414
                case HttpStatusCode.UnsupportedMediaType:           // 415
                case HttpStatusCode.RequestedRangeNotSatisfiable:   // 416
                case HttpStatusCode.ExpectationFailed:              // 417
                case HttpStatusCode.UpgradeRequired:                // 426
                    return false;

                // NotImplemented and HttpVersionNotSupported cannot be retried.
                case HttpStatusCode.NotImplemented:                 // 501
                case HttpStatusCode.HttpVersionNotSupported:        // 505
                    return false;

                // Being explicit about these in particular, they should always be retried
                case HttpStatusCode.InternalServerError:            // 500 (also used to indicate: Operation timeout)
                case HttpStatusCode.ServiceUnavailable:             // 503 (also used to indicate: Server busy)
                    return true;

                // All other status codes such as: BadGateway (502), GatewayTimeout (504) can be retried.
                default:
                    return true;
            }
        }
    }
}
