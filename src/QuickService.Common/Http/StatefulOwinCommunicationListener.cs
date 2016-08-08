// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.Common.Rest
{
    using Owin;
    using System;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Owin.Hosting;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using System.Web.Http;
    using System.Net.Http.Formatting;
    using Newtonsoft.Json;
    using System.Web.Http.Dispatcher;
    using System.Web.Http.Controllers;
    using System.Net.Http;
    using System.Reflection;
    using Diagnostics;

    #region FormatterConfig Class

    /// <summary>
    /// Configures the formatters for serialization.
    /// </summary>
    public static class FormatterConfig
    {
        /// <summary>
        /// Configuration Formatter
        /// </summary>
        /// <param name="formatters">MediaTypeFormatterCollection instances.</param>
        public static void ConfigureFormatters(MediaTypeFormatterCollection formatters)
        {
            formatters.JsonFormatter.SerializerSettings.Formatting = Formatting.Indented;
        }
    }

    #endregion

    /// <summary>
    /// OwiniCommunicationListener class access HTTP based requests.
    /// </summary>
    /// <typeparam name="TOperations">Type of the partition operations that can satisfy the requests to this controller.</typeparam>
    public class StatefulOwinCommunicationListener<TOperations> : ICommunicationListener, IHttpControllerActivator
        where TOperations : class
    {
        /// <summary>
        /// Owin server handle.
        /// </summary>
        private IDisposable _serverHandle;

        /// <summary>
        /// String containing the name of the application root.
        /// </summary>
        private string _appRoot = null;

        /// <summary>
        /// String containing the published address.
        /// </summary>
        private string _publishAddress = null;

        /// <summary>
        /// String containing the listening address.
        /// </summary>
        private string _listeningAddress = null;

        /// <summary>
        /// IServiceEventSource instance for diagnostic logging.
        /// </summary>
        private IServiceEventSource _eventSource = null;

        /// <summary>
        /// Class that implements the operations for this partition.
        /// </summary>
        private readonly TOperations _partitionOperations = default(TOperations);

        /// <summary>
        /// ServiceContext instance.
        /// </summary>
        private readonly ServiceContext _context;

        /// <summary>
        /// OwinCommunicationListener constructor.
        /// </summary>
        /// <param name="instance">Partition operation class instance.</param>
        /// <param name="context">ServiceContext instance containing information about the service instance.</param>
        /// <param name="evtSrc">IServiceEventSource to allow diagnostic logging from within this instance.</param>
        public StatefulOwinCommunicationListener(TOperations instance, ServiceContext context, IServiceEventSource evtSrc)
            : this(null, instance, context, evtSrc)
        {
        }

        /// <summary>
        /// OwinCommunicationListener constructor.
        /// </summary>
        /// <param name="appRoot">String containing the application root name of the service. Defaults to the service type name if not specified.</param>
        /// <param name="instance">Partition operation class instance.</param>
        /// <param name="context">ServiceContext instance containing information about the service instance.</param>
        /// <param name="evtSrc">IServiceEventSource to allow diagnostic logging from within this instance.</param>
        public StatefulOwinCommunicationListener(string appRoot, TOperations instance, ServiceContext context, IServiceEventSource evtSrc)
        {
            Guard.ArgumentNotNull(instance, nameof(instance));
            Guard.ArgumentNotNull(context, nameof(context));
            Guard.ArgumentNotNull(evtSrc, nameof(evtSrc));

            _appRoot = string.IsNullOrWhiteSpace(appRoot) ? context.ServiceTypeName : appRoot.TrimEnd('/');
            _partitionOperations = instance;
            _context = context;
            _eventSource = evtSrc;
        }

        /// <summary>
        /// Aborts the service communication.
        /// </summary>
        public void Abort()
        {
            StopWebServer();
            _eventSource.ServiceRequestStop(_context.ServiceTypeName, _context.PartitionId, _context.ReplicaOrInstanceId, nameof(Abort), 0);
        }

        /// <summary>
        /// Closes the service communication.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns></returns>
        public Task CloseAsync(CancellationToken cancellationToken)
        {
            StopWebServer();
            _eventSource.ServiceRequestStop(_context.ServiceTypeName, _context.PartitionId, _context.ReplicaOrInstanceId, nameof(CloseAsync), 0);
            return Task.FromResult(true);
        }

        /// <summary>
        /// Called to start communication with the service.
        /// </summary>
        /// <param name="cancellationToken">CancellationToken instance.</param>
        /// <returns>String containing the URI the service will be listening on.</returns>
        public Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            // Set the maximum number of concurrent connections. 
            // Recommendation is from http://support.microsoft.com/kb/821268.
            System.Net.ServicePointManager.DefaultConnectionLimit = 12 * Environment.ProcessorCount;

            EndpointResourceDescription serviceEndpoint = _context.CodePackageActivationContext.GetEndpoint("OwinServiceEndpoint");
            int port = serviceEndpoint.Port;
            string protocol = Enum.GetName(typeof(EndpointProtocol), serviceEndpoint.Protocol).ToLowerInvariant();

            if (_context is StatefulServiceContext)
            {
                StatefulServiceContext ssc = (StatefulServiceContext) _context;

                _listeningAddress = String.Format(
                    CultureInfo.InvariantCulture,
                    "{0}://+:{1}/{2}/{3}/{4}/{5}",
                    protocol,
                    port,
                    _appRoot,
                    ssc.PartitionId,
                    ssc.ReplicaId,
                    Guid.NewGuid());
            }
            else if (_context is StatelessServiceContext)
            {
                _listeningAddress = String.Format(CultureInfo.InvariantCulture, "{0}://+:{1}/{2}", protocol, port, _appRoot);
            }
            else
            {
                _eventSource.ServiceRequestFailed(_context.ServiceTypeName, _context.PartitionId, _context.ReplicaOrInstanceId, nameof(OpenAsync), $"Unknown ServiceContext type '{_context.GetType().FullName}' in service type '{_context.ServiceTypeName}'.");
                throw new InvalidOperationException();
            }

            _publishAddress = this._listeningAddress.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN);

            try
            {
                _eventSource.CreateCommunicationListener(_context.ServiceTypeName, _context.PartitionId, _context.ReplicaOrInstanceId, _listeningAddress);
                _serverHandle = WebApp.Start(_listeningAddress, StartupConfiguration);
                return Task.FromResult(_publishAddress);
            }
            catch (Exception ex)
            {
                _eventSource.ServiceRequestFailed(_context.ServiceTypeName, _context.PartitionId, _context.ReplicaOrInstanceId, nameof(OpenAsync), ex.ToString());
                this.StopWebServer();
                throw;
            }
        }

        /// <summary>
        /// Stops the Owin based web server from listing.
        /// </summary>
        private void StopWebServer()
        {
            if (null != _serverHandle)
            {
                try
                {
                    this._serverHandle.Dispose();
                }
                catch (ObjectDisposedException) { }
            }
        }

        /// <summary>
        /// Called when starting up the OWIN-based app, can be used to override the default configuration.
        /// </summary>
        /// <param name="app">IAppBuilder instance.</param>
        private void StartupConfiguration(IAppBuilder app)
        {
            HttpConfiguration config = new HttpConfiguration();
            FormatterConfig.ConfigureFormatters(config.Formatters);

            config.MapHttpAttributeRoutes();

            // Replace the default controller activator (to support optional injection of the stateless service into the controllers)
            config.Services.Replace(typeof(IHttpControllerActivator), this);

            config.EnsureInitialized(); // This throws a binding exception.

            app.UseWebApi(config);
        }

        #region IHttpControllerActivator interface

        /// <summary>
        /// Called to activate an instance of HTTP controller in the WebAPI pipeline
        /// </summary>
        /// <param name="request">HTTP request that triggered</param>
        /// <param name="controllerDescriptor">Description of the controller that was selected</param>
        /// <param name="controllerType">The type of the controller that was selected for this request</param>
        /// <returns>An instance of the selected HTTP controller</returns>
        /// <remarks>This is a cheap way to avoid a framework such as Unity. If already using Unity, that is a better approach.</remarks>
        public IHttpController Create(HttpRequestMessage request, HttpControllerDescriptor controllerDescriptor, Type controllerType)
        {
            Guard.ArgumentNotNull(controllerType, nameof(controllerType));

            // If the controller defines a constructor with a single parameter of the type which implements the service type, create a new instance and inject this._serviceInstance
            ConstructorInfo ci = controllerType.GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, CallingConventions.HasThis, new[] { typeof(TOperations) }, new ParameterModifier[0]);
            if (null != ci)
            {
                object[] args = new object[1] { _partitionOperations };
                return ci.Invoke(args) as IHttpController;
            }

            // If no matching constructor was found, just call the default parameter-less constructor 
            return Activator.CreateInstance(controllerDescriptor.ControllerType) as IHttpController;
        }

        #endregion
    }
}
