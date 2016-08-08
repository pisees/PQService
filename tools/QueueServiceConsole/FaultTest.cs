// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using System;
using System.Threading.Tasks;
using System.Fabric;
using System.ComponentModel;
using System.Fabric.Testability.Scenario;
using System.Threading;
using QuickService.Common.ErrorHandling;

namespace QueueServiceConsole
{
    /// <summary>
    /// Fault injection class
    /// </summary>
    internal sealed class FaultTest
    {
        /// <summary>
        /// Type of test.
        /// </summary>
        public enum TestType : byte { Unknown, Chaos, Failover }

        /// <summary>
        /// Type of test.
        /// </summary>
        private readonly TestType _type;

        /// <summary>
        /// Test duration.
        /// </summary>
        private readonly TimeSpan _duration;

        /// <summary>
        /// FabricClient instance.
        /// </summary>
        private readonly FabricClient _client;

        /// <summary>
        /// FaultTest constructor.
        /// </summary>
        /// <param name="type">Test type to run.</param>
        /// <param name="clusterEndpoint">String containing the cluster endpoint. Default is 'localhost:19000'.</param>
        public FaultTest(TestType type, TimeSpan duration, string clusterEndpoint = null)
        {
            _type = type;
            _duration = duration;
            _client = new FabricClient(clusterEndpoint ?? "localhost:19000");              
        }

        /// <summary>
        /// Runs the test.
        /// </summary>
        /// <param name="serviceName">Uri containing the service name.</param>
        /// <param name="handler">Progress changed handler. Default is null.</param>
        /// <param name="token">CancellationToken instance.</param>
        /// <returns>Task instance.</returns>
        public async Task RunAsync(Uri serviceName, ProgressChangedEventHandler handler = null, CancellationToken token = default(CancellationToken))
        {
            TimeSpan stabilization = TimeSpan.FromMinutes(10);

            switch(_type)
            {
                case TestType.Failover:
                    await ExecuteFailoverTestScenarioAsync(serviceName, (null == handler) ? ProgressHandler : handler, stabilization, token);
                    break;

                case TestType.Chaos:
                    await ExecuteChaosTestScenarioAsync(serviceName, (null == handler) ? ProgressHandler : handler, stabilization, token);
                    break;

                case TestType.Unknown:
                default:
                    throw new InvalidProgramException("Unknown test type.");
            }
        }

        /// <summary>
        /// Executes a fail over test scenario.
        /// </summary>
        /// <param name="serviceName">Uri containing the service name.</param>
        /// <param name="handler">Progress changed handler.</param>
        /// <param name="duration">Duration of the test.</param>
        /// <param name="stabilization">Duration of the stabilization period.</param>
        /// <param name="token">CancellationToken instance.</param>
        /// <returns>Task instance.</returns>
        private async Task ExecuteFailoverTestScenarioAsync(Uri serviceName, ProgressChangedEventHandler handler, TimeSpan stabilization, CancellationToken token)
        {
            Console.WriteLine($"Starting Failover scenario test on {serviceName.AbsoluteUri} lasting {_duration.TotalMinutes} minutes.");

            FailoverTestScenarioParameters ftsp = new FailoverTestScenarioParameters(PartitionSelector.RandomOf(serviceName), _duration, stabilization);

            FailoverTestScenario fts = new FailoverTestScenario(_client, ftsp);
            fts.ProgressChanged += handler;

            try
            {
                await fts.ExecuteAsync(token);
            }
            catch(AggregateException ex) { CommonExceptionHandler.OutputInnerExceptions(ex); }
            catch(Exception ex) { Console.WriteLine($"FaultTest.RunAsync Exception: {ex.Message} at {ex.StackTrace}"); }
        }

        /// <summary>
        /// Executes a chaos over test scenario.
        /// </summary>
        /// <param name="serviceName">Uri containing the service name.</param>
        /// <param name="handler">Progress changed handler.</param>
        /// <param name="duration">Duration of the test.</param>
        /// <param name="stabilization">Duration of the stabilization period.</param>
        /// <param name="token">CancellationToken instance.</param>
        /// <returns>Task instance.</returns>
        private async Task ExecuteChaosTestScenarioAsync(Uri serviceName, ProgressChangedEventHandler handler, TimeSpan stabilization, CancellationToken token)
        {
            const int MaxConcurrentFaults = 3;
            const bool EnableMoveReplicaFaults = true;

            Console.WriteLine($"Starting chaos scenario test on {serviceName.AbsoluteUri} lasting {_duration.TotalMinutes} minutes.");

            ChaosTestScenarioParameters ctsp = new ChaosTestScenarioParameters(stabilization, MaxConcurrentFaults, EnableMoveReplicaFaults, _duration);
            ChaosTestScenario cts = new ChaosTestScenario(_client, ctsp);
            cts.ProgressChanged += handler;

            try
            {
                await cts.ExecuteAsync(token);
            }
            catch (AggregateException ex) { CommonExceptionHandler.OutputInnerExceptions(ex); }
            catch (Exception ex) { Console.WriteLine($"FaultTest.RunAsync Exception: {ex.Message} at {ex.StackTrace}"); }
        }

        /// <summary>
        /// Default progress handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void ProgressHandler(object sender, ProgressChangedEventArgs args)
        {
            Console.Write($"\rComplete: {args.ProgressPercentage, -3}%  Test: {args.UserState.ToString(), -100}");
        }

        /// <summary>
        /// Restarts a random replica. 
        /// </summary>
        /// <param name="serviceName">Uri of the service in the format fabric:/[application]/[service name]</param>
        /// <returns>Task instance.</returns>
        public async Task RestartreplicaAsync(Uri serviceName)
        {
            PartitionSelector randomPartitionSelector = PartitionSelector.RandomOf(serviceName);
            ReplicaSelector primaryofReplicaSelector = ReplicaSelector.PrimaryOf(randomPartitionSelector);

            // Create FabricClient with connection and security information here
            await _client.FaultManager.RestartReplicaAsync(primaryofReplicaSelector, CompletionMode.Verify).ConfigureAwait(false);
        }

        /// <summary>
        /// Restarts a random node. 
        /// </summary>
        /// <param name="serviceName">Uri of the service in the format fabric:/[application]/[service name]</param>
        /// <returns>Task instance.</returns>
        public async Task RestartNodeAsync(Uri serviceName)
        {
            PartitionSelector randomPartitionSelector = PartitionSelector.RandomOf(serviceName);
            ReplicaSelector primaryofReplicaSelector = ReplicaSelector.PrimaryOf(randomPartitionSelector);

            // Create FabricClient with connection and security information here
            await _client.FaultManager.RestartNodeAsync(primaryofReplicaSelector, CompletionMode.Verify).ConfigureAwait(false);
        }
    }
}
