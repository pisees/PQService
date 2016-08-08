// ------------------------------------------------------------
//  <copyright file="ServiceEventSource.cs" Company="Microsoft Corporation">
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// </copyright>
// ------------------------------------------------------------

namespace QuickService.PriorityQueueService
{
    using Common.Diagnostics;
    using Microsoft.ServiceFabric.Services.Runtime;
    using QueueService;
    using System;
    using System.Diagnostics.Tracing;
    using System.Fabric;
    using System.Threading.Tasks;

    [EventSource(Name = "Microsoft-PriorityQueueService")]
    internal sealed class ServiceEventSource : EventSource, IQueueEventSource, IServiceEventSource
    {
        public static readonly ServiceEventSource Current = new ServiceEventSource();

        static ServiceEventSource()
        {
            // A workaround for the problem where ETW activities do not get tracked until Tasks infrastructure is initialized.
            // This problem will be fixed in .NET Framework 4.6.2.
            Task.Run(() => { }).Wait();
        }

        // Instance constructor is private to enforce singleton semantics
        private ServiceEventSource() : base() { }

        #region Keywords

        // Event keywords can be used to categorize events. 
        // Each keyword is a bit flag. A single event can be associated with multiple keywords (via EventAttribute.Keywords property).
        // Keywords must be defined as a public class named 'Keywords' inside EventSource that uses them.
        public static class Keywords
        {
            public const EventKeywords Requests = (EventKeywords)0x0001;
            public const EventKeywords ServiceInitialization = (EventKeywords)0x0002;
            public const EventKeywords Start = (EventKeywords)0x0004;
            public const EventKeywords Stop = (EventKeywords)0x0008;
            public const EventKeywords QueueOperation = (EventKeywords)0x0010;
            public const EventKeywords Health = (EventKeywords)0x0020;
        }

        #endregion

        // Define an instance method for each event you want to record and apply an [Event] attribute to it.
        // The method name is the name of the event.
        // Pass any parameters you want to record with the event (only primitive integer types, DateTime, Guid & string are allowed).
        // Each event method implementation should check whether the event source is enabled, and if it is, call WriteEvent() method to raise the event.
        // The number and types of arguments passed to every event method must exactly match what is passed to WriteEvent().
        // Put [NonEvent] attribute on all methods that do not define an event.
        // For more information see https://msdn.microsoft.com/en-us/library/system.diagnostics.tracing.eventsource.aspx

        #region Generic event definitions - Use only when a structure event isn't available.

        /// <summary>
        /// Generic event definitions only only be used where a structure event cannot be used. Structured events are easier to filter and search.
        /// </summary>

        [NonEvent]
        public void Message(string message, params object[] args)
        {
            if (this.IsEnabled())
            {
                string finalMessage = string.Format(message, args);
                Message(finalMessage);
            }
        }

        [NonEvent]
        public void Debug(string message, params object[] args)
        {
            if (this.IsEnabled())
            {
                string finalMessage = string.Format(message, args);
                Verbose(finalMessage);
            }
        }

        [NonEvent]
        public void Error(string message, params object[] args)
        {
            if (this.IsEnabled())
            {
                string finalMessage = string.Format(message, args);
                Error(finalMessage);
            }
        }

        [NonEvent]
        public void Critical(string message, params object[] args)
        {
            if (this.IsEnabled())
            {
                string finalMessage = string.Format(message, args);
                Critical(finalMessage);
            }
        }

        [NonEvent]
        public void Always(string message, params object[] args)
        {
            if (this.IsEnabled())
            {
                string finalMessage = string.Format(message, args);
                Always(finalMessage);
            }
        }

        [NonEvent]
        public void Warning(string message, params object[] args)
        {
            if (this.IsEnabled())
            {
                string finalMessage = string.Format(message, args);
                Warning(finalMessage);
            }
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

        private const int ErrorEventId = 3;
        [Event(ErrorEventId, Level = EventLevel.Error, Message = "{0}")]
        public void Error(string message)
        {
            if (this.IsEnabled())
            {
                WriteEvent(ErrorEventId, message);
            }
        }

        private const int CriticalEventId = 4;
        [Event(CriticalEventId, Level = EventLevel.Critical, Message = "{0}")]
        public void Critical(string message)
        {
            if (this.IsEnabled())
            {
                WriteEvent(CriticalEventId, message);
            }
        }

        private const int AlwaysEventId = 5;
        [Event(AlwaysEventId, Level = EventLevel.LogAlways, Message = "{0}")]
        public void Always(string message)
        {
            if (this.IsEnabled())
            {
                WriteEvent(AlwaysEventId, message);
            }
        }

        private const int WarningEventId = 6;
        [Event(WarningEventId, Level = EventLevel.Warning, Message = "{0}")]
        public void Warning(string message)
        {
            if (this.IsEnabled())
            {
                WriteEvent(WarningEventId, message);
            }
        }

        #endregion

        #region Service Related Events

        [NonEvent]
        public void ServiceMessage(StatelessService service, string message, params object[] args)
        {
            if (this.IsEnabled())
            {
                string finalMessage = string.Format(message, args);
                ServiceMessage(
                    service.Context.ServiceName.ToString(),
                    service.Context.ServiceTypeName,
                    service.Context.InstanceId,
                    service.Context.PartitionId,
                    service.Context.CodePackageActivationContext.ApplicationName,
                    service.Context.CodePackageActivationContext.ApplicationTypeName,
                    FabricRuntime.GetNodeContext().NodeName,
                    finalMessage);
            }
        }

        [NonEvent]
        public void ServiceMessage(StatefulService service, string message, params object[] args)
        {
            if (this.IsEnabled())
            {
                string finalMessage = string.Format(message, args);
                ServiceMessage(
                    service.Context.ServiceName.ToString(),
                    service.Context.ServiceTypeName,
                    service.Context.ReplicaId,
                    service.Context.PartitionId,
                    service.Context.CodePackageActivationContext.ApplicationName,
                    service.Context.CodePackageActivationContext.ApplicationTypeName,
                    FabricRuntime.GetNodeContext().NodeName,
                    finalMessage);
            }
        }

        [NonEvent]
        public void QueueMethodException(Guid partition, long replica, Exception ex)
        {
            QueueMethodExceptionInternal(partition, replica, ex.GetType().Name, ex.Message, ex.StackTrace);
        }

        [NonEvent]
        public void QueueItemOperationAborted(Guid partition, long replica, string id, Exception ex)
        {
            QueueMethodExceptionInternal(partition, replica, ex.GetType().Name, ex.Message, ex.StackTrace, $"ID={id}");
        }

        // For very high-frequency events it might be advantageous to raise events using WriteEventCore API.
        // This results in more efficient parameter handling, but requires explicit allocation of EventData structure and unsafe code.
        // To enable this code path, define UNSAFE conditional compilation symbol and turn on unsafe code support in project properties.
        private const int ServiceMessageEventId = 7;
        [Event(ServiceMessageEventId, Level = EventLevel.Informational, Message = "{7}")]
        private
#if UNSAFE
        unsafe
#endif
        void ServiceMessage(
            string serviceName,
            string serviceTypeName,
            long replicaOrInstanceId,
            Guid partitionId,
            string applicationName,
            string applicationTypeName,
            string nodeName,
            string message)
        {
#if !UNSAFE
            WriteEvent(ServiceMessageEventId, serviceName, serviceTypeName, replicaOrInstanceId, partitionId, applicationName, applicationTypeName, nodeName, message);
#else
            const int numArgs = 8;
            fixed (char* pServiceName = serviceName, pServiceTypeName = serviceTypeName, pApplicationName = applicationName, pApplicationTypeName = applicationTypeName, pNodeName = nodeName, pMessage = message)
            {
                EventData* eventData = stackalloc EventData[numArgs];
                eventData[0] = new EventData { DataPointer = (IntPtr) pServiceName, Size = SizeInBytes(serviceName) };
                eventData[1] = new EventData { DataPointer = (IntPtr) pServiceTypeName, Size = SizeInBytes(serviceTypeName) };
                eventData[2] = new EventData { DataPointer = (IntPtr) (&replicaOrInstanceId), Size = sizeof(long) };
                eventData[3] = new EventData { DataPointer = (IntPtr) (&partitionId), Size = sizeof(Guid) };
                eventData[4] = new EventData { DataPointer = (IntPtr) pApplicationName, Size = SizeInBytes(applicationName) };
                eventData[5] = new EventData { DataPointer = (IntPtr) pApplicationTypeName, Size = SizeInBytes(applicationTypeName) };
                eventData[6] = new EventData { DataPointer = (IntPtr) pNodeName, Size = SizeInBytes(nodeName) };
                eventData[7] = new EventData { DataPointer = (IntPtr) pMessage, Size = SizeInBytes(message) };

                WriteEventCore(ServiceMessageEventId, numArgs, eventData);
            }
#endif
        }

        private const int ServiceTypeRegisteredEventId = 10;
        [Event(ServiceTypeRegisteredEventId, Level = EventLevel.Informational, Message = "Service host process {0} registered service type {1}", Keywords = Keywords.ServiceInitialization)]
        public void ServiceTypeRegistered(int hostProcessId, string serviceType)
        {
            WriteEvent(ServiceTypeRegisteredEventId, hostProcessId, serviceType);
        }

        private const int ServiceHostInitializationFailedEventId = 11;
        [Event(ServiceHostInitializationFailedEventId, Level = EventLevel.Error, Message = "Service host initialization failed", Keywords = Keywords.ServiceInitialization)]
        public void ServiceHostInitializationFailed(string exception)
        {
            WriteEvent(ServiceHostInitializationFailedEventId, exception);
        }

        private const int ServiceInstanceConstructedEventId = 12;
        [Event(ServiceInstanceConstructedEventId, Level = EventLevel.Informational, Message = "Service instance of type {0} was constructed.", Keywords = Keywords.ServiceInitialization)]
        public void ServiceInstanceConstructed(string serviceName, Guid partition, long replicaOrInstance)
        {
            WriteEvent(ServiceInstanceConstructedEventId, serviceName, partition, replicaOrInstance);
        }

        // A pair of events sharing the same name prefix with a "Start"/"Stop" suffix implicitly marks boundaries of an event tracing activity.
        // These activities can be automatically picked up by debugging and profiling tools, which can compute their execution time, child activities,
        // and other statistics.
        private const int ServiceRequestStartEventId = 13;
        [Event(ServiceRequestStartEventId, Level = EventLevel.Informational, Message = "Service request '{0}' started", Keywords = Keywords.Requests | Keywords.Start)]
        public void ServiceRequestStart(string serviceTypeName, Guid partition, long replicaOrInstance, string requestTypeName)
        {
            WriteEvent(ServiceRequestStartEventId, requestTypeName, serviceTypeName, partition, replicaOrInstance);
        }

        private const int ServiceRequestStopEventId = 14;
        [Event(ServiceRequestStopEventId, Level = EventLevel.Informational, Message = "Service request '{0}' finished ({1}ms).", Keywords = Keywords.Requests | Keywords.Stop)]
        public void ServiceRequestStop(string serviceTypeName, Guid partition, long replicaOrInstance, string requestTypeName, int milliseconds)
        {
            WriteEvent(ServiceRequestStopEventId, requestTypeName, milliseconds, serviceTypeName, partition, replicaOrInstance);
        }

        private const int ServiceRequestFailedEventId = 15;
        [Event(ServiceRequestFailedEventId, Level = EventLevel.Error, Message = "Service request '{0}' failed for partition {2} replica {3} in {2}. Message: {4}.", Keywords = Keywords.Requests)]
        public void ServiceRequestFailed(string serviceTypeName, Guid partition, long replicaOrInstance, string requestTypeName, string msg)
        {
            WriteEvent(ServiceRequestFailedEventId, requestTypeName, serviceTypeName, partition, replicaOrInstance, msg);
        }

        private const int RunAsyncInvokedEventId = 16;
        [Event(RunAsyncInvokedEventId, Level = EventLevel.Informational, Message = "RunAsync invoked in service of type {0}, partition {1} replica {2}.", Keywords = Keywords.ServiceInitialization)]
        public void RunAsyncInvoked(string serviceTypeName, Guid partition, long replicaOrInstance)
        {
            WriteEvent(RunAsyncInvokedEventId, serviceTypeName, partition, replicaOrInstance);
        }

        private const int CreateCommunicationListenerEventId = 17;
        [Event(CreateCommunicationListenerEventId, Level = EventLevel.Informational, Message = "Create communication listener in service instance of type {0}. Listening on {3}.", Keywords = Keywords.ServiceInitialization)]
        public void CreateCommunicationListener(string serviceTypeName, Guid partition, long replicaOrInstance, string listenAddress)
        {
            WriteEvent(CreateCommunicationListenerEventId, serviceTypeName, listenAddress, partition, replicaOrInstance);
        }

        private const int ServicePartitionConfigurationChangedEventId = 18;
        [Event(ServicePartitionConfigurationChangedEventId, Level = EventLevel.Informational, Message = "Configuration for service {0} changed for partition {1} replica {2}.")]
        public void ServicePartitionConfigurationChanged(string serviceTypeName, Guid partition, long replicaOrInstance)
        {
            WriteEvent(ServicePartitionConfigurationChangedEventId, serviceTypeName, partition, replicaOrInstance);
        }

        private const int ServicePartitionDataLossEventId = 19;
        [Event(ServicePartitionDataLossEventId, Level = EventLevel.Critical, Message = "Potential data loss for service type {0}, partition {1} and replica {2}.")]
        public void PotentialDataLoss(string serviceTypeName, Guid partition, long replicaOrInstance)
        {
            WriteEvent(ServicePartitionDataLossEventId, serviceTypeName, partition, replicaOrInstance);
        }

        private const int ServicePartitionOpenedEventId = 20;
        [Event(ServicePartitionOpenedEventId, Level = EventLevel.Informational, Message = "OnOpenAsync invoked in service of type {0}, partition {1} replica {2}.")]
        public void OpenAsyncInvoked(string serviceTypeName, Guid partition, long replicaOrInstance)
        {
            WriteEvent(ServicePartitionOpenedEventId, serviceTypeName, partition, replicaOrInstance);
        }

        private const int ServicePartitionChangeRoleEventId = 21;
        [Event(ServicePartitionChangeRoleEventId, Level = EventLevel.Informational, Message = "OnChangedRoleAsync invoked in service of type {0}, partition {1} replica {2}. New role {3}.")]
        public void ChangeRoleAsyncInvoked(string serviceTypeName, Guid partition, long replicaOrInstance, string role)
        {
            WriteEvent(ServicePartitionChangeRoleEventId, serviceTypeName, partition, replicaOrInstance, role);
        }

        private const int ServicePartitionClosedEventId = 22;
        [Event(ServicePartitionClosedEventId, Level = EventLevel.Informational, Message = "OnClosedAsync invoked in service of type {0}, partition {1} replica {2}.")]
        public void CloseAsyncInvoked(string serviceTypeName, Guid partition, long replicaOrInstance)
        {
            WriteEvent(ServicePartitionClosedEventId, serviceTypeName, partition, replicaOrInstance);
        }

        private const int ServicePartitionAbortEventId = 23;
        [Event(ServicePartitionAbortEventId, Level = EventLevel.Informational, Message = "OnAbort invoked in service of type {0}, partition {1} replica {2}.")]
        public void AbortInvoked(string serviceTypeName, Guid partition, long replicaOrInstance)
        {
            WriteEvent(ServicePartitionAbortEventId, serviceTypeName, partition, replicaOrInstance);
        }

        #endregion

        #region Structured Events

        private const int QueueItemAddedEventId = 100;
        [Event(QueueItemAddedEventId, Level = EventLevel.Informational, Message = "Item {0} was added to the queue. Partition: {1} Replica: {2} Transaction: {3}", Keywords = Keywords.QueueOperation)]
        public void QueueItemAdded(long txid, Guid partition, long replica, string id)
        {
            this.WriteEvent(QueueItemAddedEventId, id, partition, replica, txid);
        }

        private const int QueueItemLeasedEventId = 101;
        [Event(QueueItemLeasedEventId, Level = EventLevel.Informational, Message = "Item {0} was leased. Time spent in queue {1}ms. Partition: {2} Replica: {3} Transaction: {4}", Keywords = Keywords.QueueOperation)]
        public void QueueItemLeased(long txid, Guid partition, long replica, string id, int duration)
        {
            this.WriteEvent(QueueItemLeasedEventId, id, duration, partition, replica, txid);
        }

        private const int QueueItemLeaseCompleteEventId = 102;
        [Event(QueueItemLeaseCompleteEventId, Level = EventLevel.Informational, Message = "Item {0} leased was completed with success={1}. Time in queue and lease {2}ms. Partition: {3} Replica: {4} Transaction: {5}", Keywords = Keywords.QueueOperation)]
        public void QueueItemLeaseComplete(long txid, Guid partition, long replica, string id, bool succeeded, int duration)
        {
            WriteEvent(QueueItemLeaseCompleteEventId, id, succeeded, duration, partition, replica, txid);
        }

        private const int QueueItemOperationAbortedEventId = 103;
        [Event(QueueItemOperationAbortedEventId, Level = EventLevel.Error, Message = "An operation on item {2} was aborted. {3}. Partition: {0} Replica: {1}", Keywords = Keywords.QueueOperation)]
        public void QueueItemOperationAborted(Guid partition, long replica, string id, string msg)
        {
            this.WriteEvent(QueueItemOperationAbortedEventId, partition, replica, id, msg);
        }

        private const int QueueItemExpiredEventId = 104;
        [Event(QueueItemExpiredEventId, Level = EventLevel.LogAlways, Message = "Item {0} expired. Partition: {1} Replica: {2} TransactionId: {3}", Keywords = Keywords.QueueOperation)]
        public void QueueItemExpired(long txid, Guid partition, long replica, string id)
        {
            this.WriteEvent(QueueItemExpiredEventId, id, partition, replica, txid);
        }

        private const int QueueCapacityEventId = 105;
        [Event(QueueCapacityEventId, Level = EventLevel.LogAlways, Message = "Queue capacity is in {2}. Current count {3}. Partition: {0} Replica: {1}", Keywords = Keywords.Health)]
        public void QueueCapacity(Guid partition, long replica, string msg, int count)
        {
            this.WriteEvent(QueueCapacityEventId, partition, replica, msg, count);
        }

        private const int QueueMethodStartEventId = 106;
        [Event(QueueMethodStartEventId, Level = EventLevel.LogAlways, Message = "QueueMethod {2} called with {3}. Partition: {0} Replica: {1}", Keywords = Keywords.QueueOperation | Keywords.Start)]
        public void QueueMethodStart(Guid partition, long replica, string name, string msg = "no message")
        {
            this.WriteEvent(QueueMethodStartEventId, partition, replica, name, msg);
        }

        private const int QueueMethodExceptionEventId = 107;
        [Event(QueueMethodExceptionEventId, Level = EventLevel.LogAlways, Message = "Exception {2} : {3} at {4}. {5}. Partition: {0} Replica: {1}", Keywords = Keywords.QueueOperation)]
        internal void QueueMethodExceptionInternal(Guid partition, long replica, string name, string exMsg, string stack, string msg = "no message")
        {
            this.WriteEvent(QueueMethodExceptionEventId, partition, replica, name, exMsg, stack, msg);
        }

        private const int QueueMethodFailureEventId = 108;
        [Event(QueueMethodFailureEventId, Level = EventLevel.Critical, Message = "Method {2} {3}. Partition: {0} Replica: {1}", Keywords = Keywords.QueueOperation)]
        public void QueueMethodFailed(Guid partition, long replica, string name, string msg = "no message")
        {
            this.WriteEvent(QueueMethodFailureEventId, partition, replica, name, msg);
        }

        private const int QueueMethodLoggingEventId = 109;
        [Event(QueueMethodLoggingEventId, Level = EventLevel.Informational, Message = "Method {0} on partition {1} replica {2} in {3}ms. Message: {4}", Keywords = Keywords.QueueOperation | Keywords.Stop)]
        public void QueueMethodLogging(string method, Guid partition, long replica, long duration, string msg = "no message")
        {
            this.WriteEvent(QueueMethodLoggingEventId, method, partition, replica, duration, msg);
        }

        private const int QueueHealthEventId = 110;
        [Event(QueueHealthEventId, Level = EventLevel.LogAlways, Message = "Queue partition {0} health is {1}. {2}", Keywords = Keywords.Health)]
        public void QueueHealth(Guid partition, string state, string msg)
        {
            this.WriteEvent(QueueHealthEventId, partition, state, msg);
        }

        private const int UnhandledExceptionEventId = 111;
        [Event(UnhandledExceptionEventId, Level = EventLevel.LogAlways, Message = "Unhandled exception: {0} - {1} at {2}", Keywords = Keywords.QueueOperation)]
        public void UnhandledException(string name, string msg, string stack)
        {
            this.WriteEvent(UnhandledExceptionEventId, name, msg, stack);
        }

        private const int UnobservedTaskExceptionEventId = 112;
        [Event(UnobservedTaskExceptionEventId, Level = EventLevel.LogAlways, Message = "Unobserved task exception: {0} - {1} at {2}", Keywords = Keywords.QueueOperation)]
        public void UnobservedTaskException(string name, string msg, string stack)
        {
            this.WriteEvent(UnobservedTaskExceptionEventId, name, msg, stack);
        }

        private const int QueueItemLeaseExtendedEventId = 113;
        [Event(QueueItemLeaseExtendedEventId, Level = EventLevel.Verbose, Message = "Lease extended by {0} seconds for item {1}. Partition: {2} Replica: {3}", Keywords = Keywords.QueueOperation)]
        public void QueueItemLeaseExtended(double duration, string key, Guid partition, long replica)
        {
            this.WriteEvent(QueueItemLeaseExtendedEventId, duration, key, partition, replica);
        }

        private const int QueueItemNotFoundEventId = 114;
        [Event(QueueItemNotFoundEventId, Level = EventLevel.Verbose, Message = "Item {0} was not found. Partition: {1} Replica: {2} ", Keywords = Keywords.QueueOperation)]
        public void QueueItemNotFound(string key, Guid partition, long replica)
        {
            this.WriteEvent(QueueItemNotFoundEventId, key, partition, replica);
        }

        private const int QueueItemRemovedEventId = 115;
        [Event(QueueItemRemovedEventId, Level = EventLevel.Verbose, Message = "Item {0} was removed. Partition: {1} Replica: {2} ", Keywords = Keywords.QueueOperation)]
        public void QueueItemRemoved(string key, Guid partition, long replica)
        {
            this.WriteEvent(QueueItemRemovedEventId, key, partition, replica);
        }

        private const int QueueItQueueItemNotPresentInItemsEventId = 116;
        [Event(QueueItQueueItemNotPresentInItemsEventId, Level = EventLevel.Verbose, Message = "Item {0} was dequeued, but not present in the item collection. Partition: {1} Replica: {2} ", Keywords = Keywords.QueueOperation)]
        public void QueueItemNotPresentInItems(string key, Guid partition, long replica)
        {
            this.WriteEvent(QueueItQueueItemNotPresentInItemsEventId, key, partition, replica);
        }

        private const int QueueItemInvalidLeaseEventId = 117;
        [Event(QueueItemInvalidLeaseEventId, Level = EventLevel.Verbose, Message = "Leased item {0} was found, but no longer in queue. Partition: {1} Replica: {2} ", Keywords = Keywords.QueueOperation)]
        public void QueueItemInvalidLease(string key, Guid partition, long replica)
        {
            this.WriteEvent(QueueItemInvalidLeaseEventId, key, partition, replica);
        }

        private const int QueueTransactionCommittedEventId = 118;
        [Event(QueueTransactionCommittedEventId, Level = EventLevel.Verbose, Message = "Transaction committed. Partition: {0} Replica: {1} Transaction: {2} Commit sequence number: {3}", Keywords = Keywords.QueueOperation)]
        public void QueueTransactionCommitted(long txid, Guid partition, long replica, long sequence)
        {
            this.WriteEvent(QueueTransactionCommittedEventId, partition, replica, txid, sequence);
        }

        #endregion

        #region Private methods
#if UNSAFE
        private int SizeInBytes(string s)
        {
            if (s == null)
            {
                return 0;
            }
            else
            {
                return (s.Length + 1) * sizeof(char);
            }
        }
#endif
        #endregion
    }
}
