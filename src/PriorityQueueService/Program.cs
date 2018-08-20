// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.PriorityQueueService
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.ServiceFabric.Services.Runtime;

    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                // Setup unhandled exception handlers.
                TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

                // The ServiceManifest.XML file defines one or more service type names.
                // RegisterServiceType maps a service type name to a .NET class.
                // When Service Fabric creates an instance of this service type,
                // an instance of the class is created in this host process.
                ServiceRuntime.RegisterServiceAsync(PriorityQueueService.ServiceTypeName, 
                    context => new PriorityQueueService(context)).GetAwaiter().GetResult();

                ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(PriorityQueueService).Name);

                // Prevents this host process from terminating to keep the service host process running.
                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
                throw;
            }
        }

        /// <summary>
        /// Event Handler delegate to log if an unhandled AppDomain exception occurs.
        /// </summary>
        /// <param name="sender">the sender</param>
        /// <param name="e">the exception details</param>
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            ServiceEventSource.Current.UnhandledException(ex.GetType().Name, ex.Message, ex.StackTrace);
        }

        /// <summary>
        /// Event Handler delegate to log if an unobserved task exception occurs.
        /// </summary>
        /// <param name="sender">the sender</param>
        /// <param name="e">the exception details</param>
        /// <remarks>
        /// We intentionally do not mark the exception as Observed, which would prevent the process from being terminated.
        /// We want the unobserved exception to take out the process. Note, as of .NET 4.5 this relies on the ThrowUnobservedTaskExceptions
        /// runtime configuration in the host App.Config settings.
        /// </remarks>
        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            ServiceEventSource.Current.UnobservedTaskException(e.Exception?.GetType().Name, e.Exception?.Message,  e.Exception?.StackTrace);

            AggregateException flattened = e.Exception?.Flatten();
            foreach (Exception ex in flattened?.InnerExceptions)
            {
                ServiceEventSource.Current.UnobservedTaskException(ex.GetType().Name, ex.Message, ex.StackTrace);
            }

            // Marking as observed to prevent process exit.
            //e.SetObserved();
        }
    }
}
