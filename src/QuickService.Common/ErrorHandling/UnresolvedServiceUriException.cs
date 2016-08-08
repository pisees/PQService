// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace ServiceFabric.Archetype.Common.ErrorHandling
{
    using System;
    using System.Net;
    using System.Runtime.CompilerServices;
    using System.Fabric;
    using System.Fabric.Health;
    /// <summary>
    /// This exception should be used when we failed to find or resolve endpoint Uri for the Fabric service
    /// Proper retry logic should catch this exception (see RestApiErrorDetectionStrategy and ServiceSessionBase class) and call to RefreshServiceUri
    /// </summary>
    [Serializable]
    public class UnresolvedServiceUriException : FabricTransientException
    {
        public UnresolvedServiceUriException(string serviceUri,
                [CallerLineNumber] int line = 0,
                [CallerMemberName] string functionName = "",
                [CallerFilePath] string sourceFile = "")
            : base($"Failed to resolve endpoint Uri for service {serviceUri}", HttpStatusCode.BadGateway, HealthState.Warning, null, line, functionName, sourceFile)
        {
        }
    }
}
