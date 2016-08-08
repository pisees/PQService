// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.QueueClient
{
    using System;
    using System.Threading.Tasks;
    using System.Diagnostics.Tracing;

    /// <summary>
    /// Event source for the QueueClient.
    /// </summary>
    [EventSource(Name = "Microsoft-PriorityQueueClient")]
    internal sealed class ClientEventSource : EventSource
    {
        public static readonly ClientEventSource Current = new ClientEventSource();

        static ClientEventSource()
        {
            // A workaround for the problem where ETW activities do not get tracked until Tasks infrastructure is initialized.
            // This problem will be fixed in .NET Framework 4.6.2.
            Task.Run(() => { }).Wait();
        }

        private const int MessageEventId = 1;
        [Event(MessageEventId, Level = EventLevel.Informational, Message = "{0}")]
        public void Message(string message)
        {
            if (this.IsEnabled())
            {
                WriteEvent(MessageEventId, message);
            }
        }


        private const int VerboseEventId = 2;
        [Event(VerboseEventId, Level = EventLevel.Verbose, Message = "{0}")]
        public void Verbose(string message)
        {
            if (this.IsEnabled())
            {
                WriteEvent(VerboseEventId, message);
            }
        }

        private const int OperationFailedEventId = 3;
        [Event(OperationFailedEventId, Level = EventLevel.Error, Message = "Encountered exception {0} during {1}.")]
        public void OperationFailed(string exception, string operation)
        {
            WriteEvent(OperationFailedEventId, exception, operation);
        }

        private const int QueueOperationResultEventId = 100;
        [Event(QueueOperationResultEventId, Level = EventLevel.Error, Message = "Completed operation {0} in {1}ms with status code {2}.")]
        public void QueueOperationResult(string operation, int duration, int statusCode, Guid id)
        {
            this.WriteEventWithRelatedActivityId(OperationFailedEventId, id, operation, duration, statusCode);
        }
    }
}
