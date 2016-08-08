// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QuickService.Common.Configuration
{
    using System;
    using System.Security;

    /// <summary>
    /// ConfigurationProvider interface.
    /// </summary>
    public interface IConfigurationProvider<TConfiguration>
    {
        /// <summary>
        /// Gets the configuration settings values.
        /// </summary>
        TConfiguration Config { get; }

        /// <summary>
        /// Get a configuration value
        /// </summary>
        /// <param name="key">Name of the configuration item. Key value is case sensitive.</param>
        /// <param name="defaultValue">Default value to return.</param>
        /// <returns>Value of the configuration setting or the default value.</returns>
        string GetConfigurationValue(string key, string defaultValue);

        /// <summary>
        /// Gets an encrypted configuration value.
        /// </summary>
        /// <param name="key">Name of the configuration item. Key value is case sensitive.</param>
        /// <param name="defaultValue">Default value to return.</param>
        /// <returns>Value of the configuration setting.</returns>
        SecureString GetEncryptedConfigurationValue(string key, string defaultValue);

        /// <summary>
        /// Gets a configuration value as an integer value.
        /// </summary>
        /// <param name="key">Name of the configuration item. Key value is case sensitive.</param>
        /// <param name="defaultValue">Default value to return.</param>
        /// <returns>Value of the configuration item or the default value.</returns>
        int GetConfigurationValue(string key, int defaultValue);

        /// <summary>
        /// Gets a configuration value as an long value.
        /// </summary>
        /// <param name="key">Name of the configuration item. Key value is case sensitive.</param>
        /// <param name="defaultValue">Default value to return.</param>
        /// <returns>Value of the configuration item or the default value.</returns>
        long GetConfigurationValue(string key, long defaultValue);

        /// <summary>
        /// Gets a configuration value as an double value.
        /// </summary>
        /// <param name="key">Name of the configuration item. Key value is case sensitive.</param>
        /// <param name="defaultValue">Default value to return.</param>
        /// <returns>Value of the configuration item or the default value.</returns>
        double GetConfigurationValue(string key, double defaultValue);

        /// <summary>
        /// Gets a configuration value as an bool value.
        /// </summary>
        /// <param name="key">Name of the configuration item. Key value is case sensitive.</param>
        /// <param name="defaultValue">Default value to return.</param>
        /// <returns>Value of the configuration item or the default value.</returns>
        bool GetConfigurationValue(string key, bool defaultValue);

        /// <summary>
        /// Gets a configuration value as an DateTime value.
        /// </summary>
        /// <param name="key">Name of the configuration item. Key value is case sensitive.</param>
        /// <param name="defaultValue">Default value to return.</param>
        /// <returns>Value of the configuration item or the default value.</returns>
        DateTime GetConfigurationValue(string key, DateTime defaultValue);

        /// <summary>
        /// Gets a configuration value as an DateTimeOffset value.
        /// </summary>
        /// <param name="key">Name of the configuration item. Key value is case sensitive.</param>
        /// <param name="defaultValue">Default value to return.</param>
        /// <returns>Value of the configuration item or the default value.</returns>
        DateTimeOffset GetConfigurationValue(string key, DateTimeOffset defaultValue);

        /// <summary>
        /// Gets a configuration value as an TimeSpan value.
        /// </summary>
        /// <param name="key">Name of the configuration item. Key value is case sensitive.</param>
        /// <param name="defaultValue">Default value to return.</param>
        /// <returns>Value of the configuration item or the default value.</returns>
        TimeSpan GetConfigurationValue(string key, TimeSpan defaultValue);
    }
}
