// ------------------------------------------------------------
//  <copyright file="ConfigurationProvider.cs" Company="Microsoft Corporation">
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// </copyright>
// ------------------------------------------------------------

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("QueueServiceTests")]

namespace QuickService.Common.Configuration
{
    using Diagnostics;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Fabric;
    using System.Fabric.Description;
    using System.IO;
    using System.Linq;
    using System.Security;
    using System.Security.Cryptography;
    using System.Text;

    #region ConfigurationClassChangedEventArgs class

    /// <summary>
    /// Configuration class changed event arguments.
    /// </summary>
    /// <typeparam name="TConfiguration">Type of configuration.</typeparam>
    public sealed class ConfigurationClassChangedEventArgs<TConfiguration> : EventArgs
        where TConfiguration : class
    {
        /// <summary>
        /// Name of the changed configuration.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Configuration class instance.
        /// </summary>
        public readonly TConfiguration CurrentConfiguration;

        /// <summary>
        /// Configuration class instance.
        /// </summary>
        public readonly TConfiguration NewConfiguration;

        /// <summary>
        /// ConfigurationClassChangeEventArgs constructor.
        /// </summary>
        /// <param name="name">Name of the changed configuration.</param>
        /// <param name="currentConfig">Current configuration instance of type TConfiguration.</param>
        /// <param name="newConfig">New configuration instance of type TConfiguration.</param>
        public ConfigurationClassChangedEventArgs(string name, TConfiguration currentConfig, TConfiguration newConfig)
        {
            Name = name;
            CurrentConfiguration = currentConfig;
            NewConfiguration = newConfig;
        }
    }

    #endregion

    /// <summary>
    /// Generic configuration provider.
    /// </summary>
    public sealed class ConfigurationProvider<TConfiguration> : IConfigurationProvider<TConfiguration>
        where TConfiguration : class
    {
        /// <summary>
        /// Name of the configuration package object.
        /// </summary>
        const string c_ConfigurationPackageObjectName = "Config";

        /// <summary>
        /// Configuration changed event for property based configuration changes.
        /// </summary>
        public event EventHandler OnConfigurationPropertyChangedEvent;

        /// <summary>
        /// Configuration changed event for property based configuration changes.
        /// </summary>
        public event EventHandler<ConfigurationClassChangedEventArgs<TConfiguration>> OnConfigurationClassChangedEvent;

        #region Members

        /// <summary>
        /// Uri of the service type instance.
        /// </summary>
        private readonly Uri _serviceNameUri = null;

        /// <summary>
        /// Name of the service type instance.
        /// </summary>
        private readonly string _serviceName = null;

        /// <summary>
        /// Unique identifier for this partition.
        /// </summary>
        private readonly Guid _partitionId;

        /// <summary>
        /// Uniquely identifies this replica or instance.
        /// </summary>
        private readonly long _replicaOrInstanceId;

        /// <summary>
        /// Current configuration settings.
        /// </summary>
        internal TConfiguration _configFile = default(TConfiguration);

        /// <summary>
        /// MD5 hash for the configuration file.
        /// </summary>
        internal byte[] _configFileHash = null;

        /// <summary>
        /// ConfigurationSection for this service type.
        /// </summary>
        private ConfigurationSection _configSection = null;

        /// <summary>
        /// IServiceEventSource instance used for logging.
        /// </summary>
        private IServiceEventSource _eventSource = null;

        /// <summary>
        /// MD5 hash for the configuration section.
        /// </summary>
        internal byte[] _configSectionHash = null;

        /// <summary>
        /// Gets the configuration settings values.
        /// </summary>
        public TConfiguration Config => _configFile;

        #endregion

        #region Constructors

        /// <summary>
        /// ConfigurationProvider for testing use only.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        internal ConfigurationProvider(Uri serviceName)
        {
            _serviceNameUri = serviceName;
            _serviceName = _serviceNameUri.Segments[_serviceNameUri.Segments.Length - 1];
        }

        /// <summary>
        /// ConfigurationProvider for testing use only.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="path">String containing the file path to load configuration from.</param>
        /// <param name="sections">ConfigurationSections.</param>
        internal ConfigurationProvider(Uri serviceName, string path, KeyedCollection<string, ConfigurationSection> sections)
        {
            _serviceNameUri = serviceName;
            _serviceName = _serviceNameUri.Segments[_serviceNameUri.Segments.Length - 1];

            LoadConfiguration(path, sections);
        }

        /// <summary>
        /// ConfigurationProvider constructor.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="context">CodePackageActivationContext instance.</param>
        /// <param name="eventSource">IServiceEventSource instance for logging.</param>
        /// <param name="partition">Partition identifier.</param>
        /// <param name="replica">Replica or instance identifier.</param>
        public ConfigurationProvider(Uri serviceName, ICodePackageActivationContext context, IServiceEventSource eventSource, Guid partition, long replica)
        {
            Guard.ArgumentNotNull(serviceName, nameof(serviceName));
            Guard.ArgumentNotNull(context, nameof(context));

            _serviceNameUri = serviceName;
            _serviceName = _serviceNameUri.Segments[_serviceNameUri.Segments.Length - 1];
            _eventSource = eventSource;
            _partitionId = partition;
            _replicaOrInstanceId = replica;

            // Subscribe to configuration change events if the context was passed. It will not be passed for unit tests.
            if (null != context)
            {
                context.ConfigurationPackageAddedEvent += CodePackageActivationContext_ConfigurationPackageAddedEvent;
                context.ConfigurationPackageModifiedEvent += CodePackageActivationContext_ConfigurationPackageModifiedEvent;
                context.ConfigurationPackageRemovedEvent += Context_ConfigurationPackageRemovedEvent;
            }

            // Configuration has already been loaded by the time we subscribe to the events above, initialize the configuration the first time.
            IList<string> names = context.GetConfigurationPackageNames();
            ConfigurationPackage pkg = context.GetConfigurationPackageObject(c_ConfigurationPackageObjectName);

            // Create the add event parameters and call.
            PackageAddedEventArgs<ConfigurationPackage> evt = new PackageAddedEventArgs<ConfigurationPackage>();
            evt.Package = pkg;
            CodePackageActivationContext_ConfigurationPackageAddedEvent(null, evt);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Loads the configuration.
        /// </summary>
        /// <param name="path">Path to the directory containing configuration information.</param>
        /// <param name="sections">Collection of keyed ConfigurationSections.</param>
        /// <remarks>Loads the configuration with the same name as the service type (e.g. [ServiceTypeName].XML. This
        /// method was constructed this way to enable some level of unit testing. ConfigurationSection cannot be created or mocked.</remarks>
        internal void LoadConfiguration(string path, KeyedCollection<string, ConfigurationSection> sections)
        {
            Guard.ArgumentNotNullOrWhitespace(path, nameof(path));
            Guard.ArgumentNotNull(sections, nameof(sections));

            _eventSource?.ServicePartitionConfigurationChanged(_serviceName, _partitionId, _replicaOrInstanceId);

            try
            {
                // Create an MD5 to hash the values to determine if they have changed.
                using (MD5 md5 = MD5.Create())
                {
                    // First load the configuration section for this service type.
                    if (sections.Contains(_serviceNameUri.AbsoluteUri))
                    {
                        // Get the section and calculate the hash.
                        ConfigurationSection section = sections[_serviceNameUri.AbsoluteUri];
                        string sectionContent = string.Join("|", section.Parameters.Select(p => $"{p.Name}~{p.Value}"));
                        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(sectionContent));

                        if (false == CompareHashes(hash, _configSectionHash))
                        {
                            // Set the provider values.
                            _configSection = section;
                            _configSectionHash = hash;

                            // If necessary, call the property changed event.
                            OnConfigurationPropertyChangedEvent?.Invoke(this, EventArgs.Empty);
                        }
                    }

                    // Second, attempt to load a file of the format <service name>.json. Use the last segment of the service name Uri for the file name.
                    string filepath = Path.Combine(path, $"{_serviceName}.json");

                    // Stream the file while calculating the MD5 hash of the contents.
                    using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                    using (CryptoStream cs = new CryptoStream(fs, md5, CryptoStreamMode.Read))
                    using (StreamReader reader = new StreamReader(cs))
                    {
                        string json = reader.ReadToEnd();
                        JsonSerializerSettings settings = new JsonSerializerSettings();
                        settings.Converters.Add(new IsoDateTimeConverter());
                        TConfiguration result = JsonConvert.DeserializeObject<TConfiguration>(json, settings);
                        byte[] fileHash = md5.Hash;
                        if (false == CompareHashes(fileHash, _configFileHash))
                        {                            
                            // If necessary, call the class changed event.
                            OnConfigurationClassChangedEvent?.Invoke(this, new ConfigurationClassChangedEventArgs<TConfiguration>(_serviceName, _configFile, result));

                            // Set the provider values.
                            _configFileHash = fileHash;
                            _configFile = result;
                        }
                    }
                }
            }
            catch (FileNotFoundException) { }
        }

        /// <summary>
        /// Compares two hash values for equality.
        /// </summary>
        /// <param name="a1">Byte array containing the hash value.</param>
        /// <param name="a2">Byte array containing the hash value.</param>
        /// <returns>True if equal, otherwise false.</returns>
        internal bool CompareHashes(byte[] a1, byte[] a2)
        {
            // If one or both are null, return false.
            if ((null == a1) || (null == a2))
                return false;

            if (a1.Length != a2.Length)
                return false;

            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[i] != a2[i])
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Called when a new configuration package has been added during a deployment.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">PackageAddedEventArgs&lt;ConfigurationPackage&gt; instance.</param>
        private void CodePackageActivationContext_ConfigurationPackageAddedEvent(object sender, PackageAddedEventArgs<ConfigurationPackage> e)
        {
            Guard.ArgumentNotNull(e, nameof(e));

            // Attempt to load the configuration.
            LoadConfiguration(e.Package.Path, e.Package.Settings.Sections);
        }

        /// <summary>
        /// Called when a change to an existing configuration package has been deployed.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">PackageAddedEventArgs&lt;ConfigurationPackage&gt; instance.</param>
        private void CodePackageActivationContext_ConfigurationPackageModifiedEvent(object sender, PackageModifiedEventArgs<ConfigurationPackage> e)
        {
            Guard.ArgumentNotNull(e, nameof(e));

            // Attempt to load the configuration. Only load the new ones, the old settings are cached within the provider.
            LoadConfiguration(e.NewPackage.Path, e.NewPackage.Settings.Sections);
        }

        /// <summary>
        /// Called when am existing configuration package has been removed.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">PackageAddedEventArgs&lt;ConfigurationPackage&gt; instance.</param>
        private void Context_ConfigurationPackageRemovedEvent(object sender, PackageRemovedEventArgs<ConfigurationPackage> e)
        {
            Guard.ArgumentNotNull(e, nameof(e));

            // Attempt to load the configuration.
            LoadConfiguration(e.Package.Path, e.Package.Settings.Sections);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get a configuration value
        /// </summary>
        /// <param name="key">Name of the configuration item. Key value is case sensitive.</param>
        /// <param name="defaultValue">Default value to return.</param>
        /// <returns>Value of the configuration setting or the default value.</returns>
        public string GetConfigurationValue(string key, string defaultValue = "")
        {
            Guard.ArgumentNotNullOrWhitespace(key, nameof(key));

            // Check if the key exists in the configuration section.
            if ((null == _configSection) || (false == _configSection.Parameters.Contains(key)))
                return defaultValue;

            // Get the property and return the value. If the item is encrypted then the encrypted string is returned without warning.
            ConfigurationProperty property = _configSection.Parameters[key];
            return property.Value;
        }

        /// <summary>
        /// Gets an encrypted configuration value.
        /// </summary>
        /// <param name="key">Name of the configuration item. Key value is case sensitive.</param>
        /// <param name="defaultValue">Default value to return.</param>
        /// <returns>Value of the configuration setting.</returns>
        /// <exception cref="InvalidOperationException">The value of the configuration item is not encrypted.</exception>
        unsafe public SecureString GetEncryptedConfigurationValue(string key, string defaultValue)
        {
            Guard.ArgumentNotNullOrWhitespace(key, nameof(key));

            // Check if the configuration provider is null, if it is return the default value.
            if ((null == _configSection) || (false == _configSection.Parameters.Contains(key)))
            {
                char[] chars = defaultValue.ToCharArray();
                fixed (char* pChars = chars)
                {
                    return new SecureString(pChars, chars.Length);
                }
            }

            // Get the property and return the value. If the property isn't encrypted, an InvalidOperationException will be thrown.
            ConfigurationProperty property = _configSection.Parameters[key];
            if (true == property.IsEncrypted)
            {
                return property.DecryptValue();
            }
            else
            {
                // Return the defaultValue, wrapped in a SecureString object; 
                // do not convert to char[] to allow for empty default strings; 
                // empty source arrays return NULL when pinned below, resulting 
                // in ArgumentNullException (in SecureString ctor). 
                fixed (char* pChars = defaultValue)
                {
                    return new SecureString(pChars, defaultValue.Length);
                }
            }
        }

        /// <summary>
        /// Gets a configuration value as an integer value.
        /// </summary>
        /// <param name="key">Name of the configuration item. Key value is case sensitive.</param>
        /// <param name="defaultValue">Default value to return.</param>
        /// <returns>Value of the configuration item or the default value.</returns>
        public int GetConfigurationValue(string key, int defaultValue)
        {
            int value;

            string sValue = this.GetConfigurationValue(key, defaultValue.ToString());
            if (false == int.TryParse(sValue, out value))
            {
                value = defaultValue;
            }
            return value;
        }

        /// <summary>
        /// Gets a configuration value as an long value.
        /// </summary>
        /// <param name="key">Name of the configuration item. Key value is case sensitive.</param>
        /// <param name="defaultValue">Default value to return.</param>
        /// <returns>Value of the configuration item or the default value.</returns>
        public long GetConfigurationValue(string key, long defaultValue)
        {
            long value;

            string sValue = this.GetConfigurationValue(key, defaultValue.ToString());
            if (false == long.TryParse(sValue, out value))
            {
                value = defaultValue;
            }
            return value;
        }

        /// <summary>
        /// Gets a configuration value as an double value.
        /// </summary>
        /// <param name="key">Name of the configuration item. Key value is case sensitive.</param>
        /// <param name="defaultValue">Default value to return.</param>
        /// <returns>Value of the configuration item or the default value.</returns>
        public double GetConfigurationValue(string key, double defaultValue)
        {
            double value;

            string sValue = this.GetConfigurationValue(key, defaultValue.ToString());
            if (false == double.TryParse(sValue, out value))
            {
                value = defaultValue;
            }
            return value;
        }

        /// <summary>
        /// Gets a configuration value as an bool value.
        /// </summary>
        /// <param name="key">Name of the configuration item. Key value is case sensitive.</param>
        /// <param name="defaultValue">Default value to return.</param>
        /// <returns>Value of the configuration item or the default value.</returns>
        public bool GetConfigurationValue(string key, bool defaultValue)
        {
            bool value;

            string sValue = this.GetConfigurationValue(key, defaultValue.ToString());
            if (false == bool.TryParse(sValue, out value))
            {
                value = defaultValue;
            }
            return value;
        }

        /// <summary>
        /// Gets a configuration value as an DateTime value.
        /// </summary>
        /// <param name="key">Name of the configuration item. Key value is case sensitive.</param>
        /// <param name="defaultValue">Default value to return.</param>
        /// <returns>Value of the configuration item or the default value.</returns>
        public DateTime GetConfigurationValue(string key, DateTime defaultValue)
        {
            DateTime value;

            string sValue = this.GetConfigurationValue(key, defaultValue.ToString("o"));
            if (false == DateTime.TryParse(sValue, out value))
            {
                value = defaultValue;
            }
            return value;
        }

        /// <summary>
        /// Gets a configuration value as an DateTimeOffset value.
        /// </summary>
        /// <param name="key">Name of the configuration item. Key value is case sensitive.</param>
        /// <param name="defaultValue">Default value to return.</param>
        /// <returns>Value of the configuration item or the default value.</returns>
        public DateTimeOffset GetConfigurationValue(string key, DateTimeOffset defaultValue)
        {
            DateTimeOffset value;

            string sValue = this.GetConfigurationValue(key, defaultValue.ToString("o"));
            if (false == DateTimeOffset.TryParse(sValue, out value))
            {
                value = defaultValue;
            }
            return value;
        }

        /// <summary>
        /// Gets a configuration value as an TimeSpan value.
        /// </summary>
        /// <param name="key">Name of the configuration item. Key value is case sensitive.</param>
        /// <param name="defaultValue">Default value to return.</param>
        /// <returns>Value of the configuration item or the default value.</returns>
        public TimeSpan GetConfigurationValue(string key, TimeSpan defaultValue)
        {
            TimeSpan value;

            string configValue = this.GetConfigurationValue(key, defaultValue.ToString("c"));
            if (false == TimeSpan.TryParse(configValue, out value))
            {
                value = defaultValue;
            }

            return value;
        }

        #endregion
    }
}
