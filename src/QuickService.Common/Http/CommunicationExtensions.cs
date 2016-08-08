// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Fabric;
using Microsoft.ServiceFabric.Services.Communication;

namespace QuickService.Common.Http
{
    /// <summary>
    /// Extensions for the HTTP communications methods.
    /// </summary>
    public static class CommunicationExtensions
    {
        /// <summary>
        /// Gets the first endpoint from the array of endpoints within a ResolvedServiceEndpoint.
        /// </summary>
        /// <param name="rse">ResolvedServiceEndpoint instance.</param>
        /// <returns>String containing the replica address.</returns>
        /// <exception cref="InvalidProgramException">ResolvedServiceEndpoint address list coudln't be parsed or no endpoints exist.</exception>
        public static string GetFirstEndpoint(this ResolvedServiceEndpoint rse)
        {
            ServiceEndpointCollection sec = null;
            if (ServiceEndpointCollection.TryParseEndpointsString(rse.Address, out sec))
            {
                string replicaAddress;
                if (sec.TryGetFirstEndpointAddress(out replicaAddress))
                {
                    return replicaAddress;
                }
            }

            throw new InvalidProgramException("ResolvedServiceEndpoint had invalid address");
        }

        /// <summary>
        /// Gets the endpoint from the array of endpoints using the listener name.
        /// </summary>
        /// <param name="rse">ResolvedServiceEndpoint instance.</param>
        /// <param name="name">Listener name.</param>
        /// <returns>String containing the replica address.</returns>
        /// <exception cref="ArgumentException">ResolvedServiceEndpoint address list coudln't be parsed.</exception>
        /// <exception cref="InvalidProgramException">ResolvedServiceEndpoint address list coudln't be parsed.</exception>
        public static string GetEndpoint(this ResolvedServiceEndpoint rse, string name)
        {
            ServiceEndpointCollection sec = null;
            if (ServiceEndpointCollection.TryParseEndpointsString(rse.Address, out sec))
            {
                string replicaAddress;
                if (sec.TryGetEndpointAddress(name, out replicaAddress))
                {
                    return replicaAddress;
                }
                else
                    throw new ArgumentException(nameof(name));
            }
            else
                throw new InvalidProgramException("ResolvedServiceEndpoint had invalid address");
        }
    }
}

