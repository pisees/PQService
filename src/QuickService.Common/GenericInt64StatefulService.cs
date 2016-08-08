// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.Common
{
    using Common;
    using Common.Configuration;
    using Common.Diagnostics;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Services.Runtime;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Fabric;
    using System.Fabric.Health;
    using System.Threading;
    using System.Threading.Tasks;

    #region RoleChangedEventArgs class

    /// <summary>
    /// Configuration class changed event arguments.
    /// </summary>
    public sealed class RoleChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Current role.
        /// </summary>
        public readonly ReplicaRole CurrentRole;

        /// <summary>
        /// New role.
        /// </summary>
        public readonly ReplicaRole NewRole;

        /// <summary>
        /// RoleChangedEventArgs constructor.
        /// </summary>
        /// <param name="currentRole">Current role.</param>
        /// <param name="newRole">New role.</param>
        public RoleChangedEventArgs(ReplicaRole currentRole, ReplicaRole newRole)
        {
            NewRole = newRole;
            CurrentRole = currentRole;
        }
    }

    #endregion

    /// <summary>
    /// Generic stateful service class.
    /// </summary>
    /// <typeparam name="TConfiguration">Type of the configuration class.</typeparam>
    public abstract class GenericInt64PartitionService<TConfiguration> : StatefulService
        where TConfiguration : class, new()
    {
        #region Fields

        /// <summary>
        /// IStatefulServiceEventSource instance.
        /// </summary>
        private readonly IServiceEventSource _eventSource = null;

        /// <summary>
        /// Node health report timer.
        /// </summary>
        private Timer _nodeHealthTimer = null;

        /// <summary>
        /// Time the last CPU sample was collected.
        /// </summary>
        private DateTimeOffset _timeOfCpuSample;

        /// <summary>
        /// TIme this process has used the CPU.
        /// </summary>
        private TimeSpan _cpuProcessTime = TimeSpan.Zero;

        /// <summary>
        /// Last memory load.
        /// </summary>
        protected uint MemoryLoad = 0;

        /// <summary>
        /// Maximum memory load before writes are no longer permitted.
        /// </summary>
        protected uint MemoryLoadMax = 90;

        /// <summary>
        /// CPU performance counter instance.
        /// </summary>
        protected PerformanceCounter _cpuCounter = null;

        /// <summary>
        /// CancellationTokenSource instance used to cancel running operations.
        /// </summary>
        protected readonly CancellationTokenSource TokenSource = null;

        /// <summary>
        /// OnRoleChanged event handler.
        /// </summary>
        public event EventHandler<RoleChangedEventArgs> OnRoleChangeEvent;

        /// <summary>
        /// OnOpen event handler.
        /// </summary>
        public event EventHandler OnOpenEvent;

        /// <summary>
        /// OnClose event handler.
        /// </summary>
        public event EventHandler OnCloseEvent;

        /// <summary>
        /// OnAbort event handler.
        /// </summary>
        public event EventHandler OnAbortEvent;

        /// <summary>
        /// Static fabric client instance used for interacting with service fabric infrastructure.
        /// </summary>
        public static readonly FabricClient ServiceFabricClient = new FabricClient();

        /// <summary>
        /// Configuration provider instance.
        /// </summary>
        public readonly ConfigurationProvider<TConfiguration> ConfigurationProvider = null;

        /// <summary>
        /// CurrentRole of this instance.
        /// </summary>
        public ReplicaRole CurrentRole { get; private set; }

        /// <summary>
        /// Name of the application.
        /// </summary>
        public readonly Uri Application;

        /// <summary>
        /// QueueServiceConfiguration instance.
        /// </summary>
        public TConfiguration Configuration => ConfigurationProvider.Config;

        /// <summary>
        /// Indicates if the partition can be read from within this process.
        /// </summary>
        public bool CanRead => base.Partition.ReadStatus == PartitionAccessStatus.Granted;

        /// <summary>
        /// Indicates if the partition can be written to from within this process.
        /// </summary>
        public bool CanWrite => ((base.Partition.WriteStatus == PartitionAccessStatus.Granted) && (MemoryLoad < MemoryLoadMax));

        /// <summary>
        /// Gets the partition offset number.
        /// </summary>
        public long PartitionOffset
        {
            get
            {
                // Cast PartitionInformation into Int64RangePartitionInfo and validate that LoKey equals HighKey. If they don't then
                // partitioning was not established as 0...N partitioning, which is required for this class.
                Int64RangePartitionInformation rpi = (Int64RangePartitionInformation)Partition.PartitionInfo;
                Guard.ArgumentIsEqual(rpi.LowKey, rpi.HighKey, "Partition is not defined as 0...N partitions.");

                // The partition offset is the value of the LowKey or HighKey within the range of 0 to partitions - 1.
                return rpi.LowKey;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// QueueService constructor.
        /// </summary>
        /// <param name="context">StatefulServiceContext instance.</param>
        /// <param name="eventSource">IStatefulServiceEventSource instance for diagnostic logging.</param>
        public GenericInt64PartitionService(StatefulServiceContext context, IServiceEventSource eventSource)
            : base(context)
        {
            // Check passed parameters.
            Guard.ArgumentNotNull(context, nameof(context));
            Guard.ArgumentNotNull(eventSource, nameof(eventSource));

            _eventSource = eventSource;

            TokenSource = new CancellationTokenSource();

            // Parse the service name into an application name.
            string appName = (Context.ServiceName.Segments.Length > 2) ? Context.ServiceName.Segments[1].Replace("/", "") : "";
            Application = new Uri($"fabric:/{appName}");

            ConfigurationProvider = new ConfigurationProvider<TConfiguration>(context.ServiceName, context.CodePackageActivationContext, eventSource, Context.PartitionId, Context.ReplicaOrInstanceId);
            _eventSource?.ServicePartitionConfigurationChanged(Context.ServiceTypeName, Context.PartitionId, Context.ReplicaOrInstanceId);
        }

        #endregion

        #region Private / Protected Methods

        /// <summary>
        /// Override this to receive a call if Service Fabric detects a possible data loss condition.
        /// </summary>
        /// <param name="restoreCtx">RestoreContext instance.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns>Task that represents the asynchronous operation. The Boolean task result indicates whether or not state was restored, 
        /// e.g. from a backup. True indicates that state was restored from an external source and false indicates that the state has not been changed.</returns>
        protected override Task<bool> OnDataLossAsync(RestoreContext restoreCtx, CancellationToken cancellationToken)
        {
            _eventSource?.PotentialDataLoss(Context.ServiceTypeName, Context.PartitionId, Context.ReplicaOrInstanceId);
            return base.OnDataLossAsync(restoreCtx, cancellationToken);
        }

        /// <summary>
        /// Called when the service instance is being aborted.
        /// </summary>
        protected override void OnAbort()
        {
            _eventSource?.AbortInvoked(Context.ServiceTypeName, Context.PartitionId, Context.ReplicaOrInstanceId);

            // Clean up.
            TokenSource?.Cancel();
            _nodeHealthTimer?.Dispose();
            _nodeHealthTimer = null;
            _cpuCounter?.Dispose();
            _cpuCounter = null;

            base.OnAbort();
        }

        /// <summary>
        /// Called when the service instance if being closed.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override Task OnCloseAsync(CancellationToken cancellationToken)
        {
            _eventSource?.CloseAsyncInvoked(Context.ServiceTypeName, Context.PartitionId, Context.ReplicaOrInstanceId);

            // Clean up.
            TokenSource?.Cancel();
            _nodeHealthTimer?.Dispose();
            _nodeHealthTimer = null;
            _cpuCounter?.Dispose();
            _cpuCounter = null;

            return base.OnCloseAsync(cancellationToken);
        }

        /// <summary>
        /// Called when the service instance changes role.
        /// </summary>
        /// <param name="newRole">New role.</param>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns></returns>
        protected override Task OnChangeRoleAsync(ReplicaRole newRole, CancellationToken cancellationToken)
        {
            _eventSource?.ChangeRoleAsyncInvoked(Context.ServiceTypeName, Context.PartitionId, Context.ReplicaOrInstanceId, newRole.ToString());

            // Let the base change the role.
            Task t = base.OnChangeRoleAsync(newRole, cancellationToken);

            // Change the role, then fire an event if necessary.
            var oldRole = CurrentRole;
            CurrentRole = newRole;
            OnRoleChangeEvent?.Invoke(this, new RoleChangedEventArgs(oldRole, CurrentRole));

            return t;            
        }

        /// <summary>
        /// Called when the service instance is started.
        /// </summary>
        /// <param name="openMode"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override Task OnOpenAsync(ReplicaOpenMode openMode, CancellationToken cancellationToken)
        {
            TimeSpan interval = TimeSpan.FromSeconds(30);
            //_nodeHealthTimer = new Timer(async (o) => { await ReportNodeHealthAndLoadAsync(interval); }, cancellationToken, interval, interval);

            // Create the global CPU performance counter and sample the CPU time.
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
            _cpuProcessTime = Process.GetCurrentProcess().TotalProcessorTime;
            _timeOfCpuSample = DateTimeOffset.UtcNow;

            _eventSource?.OpenAsyncInvoked(Context.ServiceTypeName, Context.PartitionId, Context.ReplicaOrInstanceId);
            return base.OnOpenAsync(openMode, cancellationToken);
        }

        /// <summary>
        /// This is the main entry point for your service's partition replica. 
        /// RunAsync executes when the primary replica for this partition has write status.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // Log that RunAsync started.
            _eventSource?.RunAsyncInvoked(Context.ServiceTypeName, Context.PartitionId, Context.ReplicaOrInstanceId);

            // Wait until the cancellation token is signaled.
            await Task.Delay(-1, cancellationToken);
        }

        #endregion

        #region Health Reporting Helper Methods

        /// <summary>
        /// Reports the capacity health of a collection in a partition to Service Fabric.
        /// </summary>
        /// <param name="healthSourceId">Health source identifier.</param>
        /// <param name="name">Health property name.</param>
        /// <param name="count">Current number of items in the collection.</param>
        /// <param name="capacity">Configured capacity of the collection.</param>
        /// <param name="pWarn">Warning percent of capacity.</param>
        /// <param name="pError">Error percent of capacity.</param>
        /// <param name="ttl">Health report time to live.</param>
        public void ReportHealthPartitionCapacity(string healthSourceId, string name, long count, long capacity, double pWarn, double pError, TimeSpan ttl)
        {
            Guard.ArgumentNotNullOrWhitespace(healthSourceId, nameof(healthSourceId));
            Guard.ArgumentNotNullOrWhitespace(name, nameof(name));

            // Calculate the percentages, warning and error counts.
            double percentCapacity = (0 == count || 0 == capacity) ? 0.0 : ((double)count / (double)capacity) * 100.0;
            long queueWarningCount = (long)(capacity * pWarn);
            long queueErrorCount = (long)(capacity * pError);

            // Determine the health state based on the count vs. the capacity.
            HealthState hs = (count >= queueErrorCount) ? HealthState.Error
                : ((count >= queueWarningCount) ? HealthState.Warning : HealthState.Ok);

            // Create the health information to report to Service Fabric.
            HealthInformation hi = new HealthInformation(healthSourceId, name, hs);
            hi.TimeToLive = (0.0 <= ttl.TotalMilliseconds) ? TimeSpan.FromSeconds(30) : ttl;
            hi.Description = $"Count: {count:N0}, Capacity: {capacity:N0}, Used: {percentCapacity}%";
            hi.RemoveWhenExpired = true;
            hi.SequenceNumber = HealthInformation.AutoSequenceNumber;

            // Create a partition health report.
            PartitionHealthReport phr = new PartitionHealthReport(Context.PartitionId, hi);
            ServiceFabricClient.HealthManager.ReportHealth(phr);
        }

        /// <summary>
        /// Reports the capacity health of a collection in a partition to Service Fabric.
        /// </summary>
        /// <param name="healthSourceId">Health source identifier.</param>
        /// <param name="name">Health property name.</param>
        /// <param name="count">Current number of items in the collection.</param>
        /// <param name="capacity">Configured capacity of the collection.</param>
        /// <param name="pWarn">Warning percent of capacity.</param>
        /// <param name="pError">Error percent of capacity.</param>
        /// <param name="ttl">Health report time to live.</param>
        public void ReportHealthReplicaCapacity(string healthSourceId, string name, long count, long capacity, double pWarn, double pError, TimeSpan ttl)
        {
            Guard.ArgumentNotNullOrWhitespace(healthSourceId, nameof(healthSourceId));
            Guard.ArgumentNotNullOrWhitespace(name, nameof(name));

            // Calculate the percentages, warning and error counts.
            double percentCapacity = (0 == count || 0 == capacity) ? 0.0 : ((double)count / (double)capacity) * 100.0;
            long queueWarningCount = (long)(capacity * pWarn);
            long queueErrorCount = (long)(capacity * pError);

            // Determine the health state based on the count vs. the capacity.
            HealthState hs = (count >= queueErrorCount) ? HealthState.Error
                : ((count >= queueWarningCount) ? HealthState.Warning : HealthState.Ok);

            // Create the health information to report to Service Fabric.
            HealthInformation hi = new HealthInformation(healthSourceId, name, hs);
            hi.TimeToLive = (0.0 <= ttl.TotalMilliseconds) ? TimeSpan.FromSeconds(30) : ttl;
            hi.Description = $"Count: {count:N0}, Capacity: {capacity:N0}, Used: {percentCapacity}%";
            hi.RemoveWhenExpired = true;
            hi.SequenceNumber = HealthInformation.AutoSequenceNumber;

            // Create a replica health report.
            StatefulServiceReplicaHealthReport ssrhr = new StatefulServiceReplicaHealthReport(Context.PartitionId, Context.ReplicaId, hi);
            ServiceFabricClient.HealthManager.ReportHealth(ssrhr);
        }

        /// <summary>
        /// Reports the capacity health of a collection in a partition to Service Fabric.
        /// </summary>
        /// <param name="healthSourceId">Health source identifier.</param>
        /// <param name="name">Health property name.</param>
        /// <param name="latency">AverageLatency instance.</param>
        /// <param name="warnValue">Warning value.</param>
        /// <param name="errorValue">Error value.</param>
        /// <param name="ttl">Health report time to live.</param>
        public void ReportHealthReplicaLatency(string healthSourceId, string name, AverageLatency latency, Int64 warnValue, Int64 errorValue, TimeSpan ttl)
        {
            Guard.ArgumentNotNullOrWhitespace(healthSourceId, nameof(healthSourceId));
            Guard.ArgumentNotNullOrWhitespace(name, nameof(name));

            // Determine the health state based on the count vs. the capacity.
            HealthState hs = (latency.GetLatest() >= errorValue) ? HealthState.Error
                : ((latency.GetLatest() >= warnValue) ? HealthState.Warning : HealthState.Ok);

            // Create the health information to report to Service Fabric.
            HealthInformation hi = new HealthInformation(healthSourceId, name, hs);
            hi.TimeToLive = (0.0 <= ttl.TotalMilliseconds) ? TimeSpan.FromSeconds(30) : ttl;
            hi.Description = $"{name} latency: {latency.GetLatest()}";
            hi.RemoveWhenExpired = true;
            hi.SequenceNumber = HealthInformation.AutoSequenceNumber;

            // Create a replica health report.
            StatefulServiceReplicaHealthReport ssrhr = new StatefulServiceReplicaHealthReport(Context.PartitionId, Context.ReplicaId, hi);
            ServiceFabricClient.HealthManager.ReportHealth(ssrhr);
        }

        /// <summary>
        /// Reports the requests per second as part of the capacity of a partition.
        /// </summary>
        /// <param name="healthSourceId">Health source identifier.</param>
        /// <param name="name">Health property name.</param>
        /// <param name="rps">Current requests per second value.</param>
        /// <param name="capacity">Configured capacity.</param>
        /// <param name="pWarn">Warning percent of capacity.</param>
        /// <param name="pError">Error percent of capacity.</param>
        /// <param name="ttl">Health report time to live.</param>
        public void ReportHealthRequestPerSecond(string healthSourceId, string name, long rps, long capacity = 0, double pWarn = 0.75, double pError = 0.90, TimeSpan ttl = default(TimeSpan))
        {
            Guard.ArgumentNotNullOrWhitespace(healthSourceId, nameof(healthSourceId));
            Guard.ArgumentNotNullOrWhitespace(name, nameof(name));

            // Calculate the capacity percentages.
            capacity = (capacity <= 0.0) ? long.MaxValue : capacity;
            double percentCapacity = (rps / capacity) * 100.0;
            long rpsWarningCount = (long)(capacity * pWarn);
            long rpsErrorCount = (long)(capacity * pError);

            // Determine the health state based on the count vs. the capacity.
            HealthState hs = (rps >= rpsErrorCount) ? HealthState.Error : ((rps >= rpsWarningCount) ? HealthState.Warning : HealthState.Ok);          

            // Create the health information to report to Service Fabric.
            HealthInformation hi = new HealthInformation(healthSourceId, name, hs);
            hi.TimeToLive = (0.0 <= ttl.TotalMilliseconds) ? TimeSpan.FromSeconds(30) : ttl;
            hi.Description = $"RPS: {rps:N0}.";
            hi.RemoveWhenExpired = true;
            hi.SequenceNumber = HealthInformation.AutoSequenceNumber;

            // Create a partition health report.
            PartitionHealthReport phr = new PartitionHealthReport(Context.PartitionId, hi);
            ServiceFabricClient.HealthManager.ReportHealth(phr);
        }

        /// <summary>
        /// Reports the health of a node.
        /// </summary>
        /// <param name="ttl"></param>
        public async Task ReportNodeHealthAndLoadAsync(TimeSpan ttl)
        {
            const int MB = 1048576;
            HealthInformation hi = null;
            NodeHealthReport nhr = null;

            try
            {
                // Get the global memory load and report as a node health parameter.
                NativeMethods.MEMORYSTATUSEX msex = new NativeMethods.MEMORYSTATUSEX();
                if (NativeMethods.GlobalMemoryStatus(ref msex))
                {
                    HealthState hs = (msex.dwMemoryLoad > 80) ? HealthState.Warning : (msex.dwMemoryLoad > 95) ? HealthState.Error : HealthState.Ok;

                    // Save the current memory load.
                    MemoryLoad = msex.dwMemoryLoad;

                    // Create the health information to report to Service Fabric.
                    hi = new HealthInformation("NodeHealth", "MemoryLoad", hs);
                    hi.TimeToLive = (0.0 <= ttl.TotalMilliseconds) ? TimeSpan.FromSeconds(30) : ttl;
                    hi.Description = $"Percent of memory in used on this node: {msex.dwMemoryLoad}";
                    hi.RemoveWhenExpired = true;
                    hi.SequenceNumber = HealthInformation.AutoSequenceNumber;

                    // Create a replica health report.
                    nhr = new NodeHealthReport(Context.NodeContext.NodeName, hi);
                    ServiceFabricClient.HealthManager.ReportHealth(nhr);
                }

                // Create the health information and send report to Service Fabric.
                hi = new HealthInformation("NodeHealth", "CPU", HealthState.Ok);
                hi.TimeToLive = (0.0 <= ttl.TotalMilliseconds) ? TimeSpan.FromSeconds(30) : ttl;
                hi.Description = $"Total CPU usage on this node: {_cpuCounter.NextValue()}";
                hi.RemoveWhenExpired = true;
                hi.SequenceNumber = HealthInformation.AutoSequenceNumber;
                nhr = new NodeHealthReport(Context.NodeContext.NodeName, hi);
                ServiceFabricClient.HealthManager.ReportHealth(nhr);

                // Get the number of deployed replicas on this node for this service.
                int serviceReplicaCount = 0;
                var replicaList = await ServiceFabricClient.QueryManager.GetDeployedReplicaListAsync(Context.NodeContext.NodeName, Application);
                for (int i = 0; i < replicaList.Count; i++)
                {
                    if (Context.ServiceName == replicaList[i].ServiceName)
                        serviceReplicaCount++;
                }

                DateTimeOffset oldSampleTime = _timeOfCpuSample;
                TimeSpan oldCpuSample = _cpuProcessTime;
                _cpuProcessTime = Process.GetCurrentProcess().TotalProcessorTime;
                _timeOfCpuSample = DateTimeOffset.UtcNow;

                long processTicks = (_cpuProcessTime - oldCpuSample).Ticks;
                long periodTicks = (_timeOfCpuSample - oldSampleTime).Ticks;
                long cpuTicks = (processTicks / periodTicks);
                long cpuPercent = (cpuTicks / serviceReplicaCount) * 100;
                long partitionWorkingSet = ((Process.GetCurrentProcess().WorkingSet64 / MB) / serviceReplicaCount);

                // Report the partition load metrics.
                LoadMetric[] metrics = new LoadMetric[]
                {
                    new LoadMetric("PartitionCPU", (int) cpuPercent),
                    new LoadMetric("WorkingSetMB", Convert.ToInt32(partitionWorkingSet))
                };

                ReportLoad(metrics);
            }
            catch (Exception ex)
            {
                _eventSource.ServiceRequestFailed(Context.ServiceTypeName, Context.PartitionId, Context.ReplicaId, "ReportNodeHealthAndLoadAsync", ex.Message);
            }
        }

        /// <summary>
        /// Reports the load of the partition.
        /// </summary>
        /// <param name="metrics">IEnumerable containing the set of LoadMetric instances to report.</param>
        public void ReportLoad(IEnumerable<LoadMetric> metrics)
        {
            base.Partition.ReportLoad(metrics);
        }

        #endregion
    }
}
