// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace QueueServiceTests
{
    using System.IO;
    using System.Web.Script.Serialization;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using QuickService.Common.Queue;
    using System;
    using System.Text;
    using QuickService.Common.Configuration;
    using System.Collections.ObjectModel;
    using System.Fabric.Description;
    using System.Security;
    using QuickService.PriorityQueueService;
    using QuickService.QueueService;

    /// <summary>
    /// Unit test to validate that the JSON configuration file can be parsed into the destination type. Can't use the entire
    /// framework because ConfigurationPackage, ConfigSection and Package cannot be mocked.
    /// </summary>
    [TestClass]
    public class Configuration_UnitTest
    {
        const string configPath = @"..\..";
        Uri serviceName = new Uri("fabric:/PriorityQueueSample/TestQueueService");

        bool classChanged = false;
        bool propertyChanged = false;

        [TestMethod]
        public void ConfigurationProvider_ProductionConfigurationTest()
        {
            // NOTE: This will only work for the current path. 
            const string prodPath = @"..\..\..\..\src\PriorityQueueService\PackageRoot\Config";
            Uri prodServiceName = new Uri("fabric:/PriorityQueueSample/PriorityQueueService");

            ConfigurationProvider<PriorityQueueServiceConfiguration> cp = new ConfigurationProvider<PriorityQueueServiceConfiguration>(prodServiceName);
            cp.LoadConfiguration(prodPath, new TestKeyedCollection());

            // Validate the configuration file values.
            Assert.AreEqual(1000000, cp.Config.MaxQueueCapacityPerPartition);
            Assert.AreEqual(50000, cp.Config.MaxLeaseCapacityPerPartition);
            Assert.AreEqual(1000, cp.Config.MaxExpiredCapacityPerPartition);
            Assert.AreEqual(0.80, cp.Config.CapacityWarningPercent);
            Assert.AreEqual(0.99, cp.Config.CapacityErrorPercent);
            Assert.AreEqual(5, cp.Config.MaximumDequeueCount);
            Assert.AreEqual(5, cp.Config.NumberOfQueues);
            Assert.AreEqual(0.80, cp.Config.LeaseItemPercentWarning);
            Assert.AreEqual(0.99, cp.Config.LeaseItemPercentError);
            Assert.AreEqual(TimeSpan.FromMinutes(10), cp.Config.LeaseDuration);
            Assert.AreEqual(TimeSpan.FromSeconds(30), cp.Config.HealthCheckInterval);
            Assert.AreEqual(TimeSpan.FromSeconds(4), cp.Config.FabricOperationTimeout);
            Assert.AreEqual(TimeSpan.FromDays(2), cp.Config.ItemExpiration);
        }

        [TestMethod]
        public void ConfigurationProvider_ConstructorTest()
        {
            classChanged = false;
            propertyChanged = false;
            KeyedCollection<string, ConfigurationSection> kc = new TestKeyedCollection();
            ConfigurationProvider<TestQueueServiceConfiguration> cp = new ConfigurationProvider<TestQueueServiceConfiguration>(serviceName);
            cp.OnConfigurationClassChangedEvent += Cp_OnConfigurationClassChangedEvent;
            cp.OnConfigurationPropertyChangedEvent += Cp_OnConfigurationPropertyChangedEvent;

            // Load the configuration.
            cp.LoadConfiguration(configPath, kc);

            // Validate the configuration file hash.
            Assert.AreEqual(16, cp._configFileHash.Length);
            Assert.AreEqual(87, cp._configFileHash[0]);
            Assert.AreEqual(86, cp._configFileHash[1]);
            Assert.AreEqual(254, cp._configFileHash[2]);
            Assert.AreEqual(123, cp._configFileHash[3]);
            Assert.AreEqual(89,  cp._configFileHash[4]);
            Assert.AreEqual(121, cp._configFileHash[5]);
            Assert.AreEqual(26,  cp._configFileHash[6]);
            Assert.AreEqual(230, cp._configFileHash[7]);
            Assert.AreEqual(193, cp._configFileHash[8]);
            Assert.AreEqual(23, cp._configFileHash[9]);
            Assert.AreEqual(94, cp._configFileHash[10]);
            Assert.AreEqual(13, cp._configFileHash[11]);
            Assert.AreEqual(231, cp._configFileHash[12]);
            Assert.AreEqual(156, cp._configFileHash[13]);
            Assert.AreEqual(118, cp._configFileHash[14]);
            Assert.AreEqual(39, cp._configFileHash[15]);

            // Validate the configuration file values.
            Assert.AreEqual(50000, cp.Config.MaxQueueCapacityPerPartition);
            Assert.AreEqual(15000, cp.Config.MaxLeaseCapacityPerPartition);
            Assert.AreEqual(1000, cp.Config.MaxExpiredCapacityPerPartition);
            Assert.AreEqual(0.80, cp.Config.CapacityWarningPercent);
            Assert.AreEqual(0.95, cp.Config.CapacityErrorPercent);
            Assert.AreEqual(5, cp.Config.MaximumDequeueCount);
            Assert.AreEqual(5, cp.Config.NumberOfQueues);
            Assert.AreEqual(0.10, cp.Config.LeaseItemPercentWarning);
            Assert.AreEqual(0.20, cp.Config.LeaseItemPercentError);
            Assert.AreEqual(TimeSpan.FromMinutes(2), cp.Config.LeaseDuration);
            Assert.AreEqual(TimeSpan.FromSeconds(30), cp.Config.HealthCheckInterval);
            Assert.AreEqual(TimeSpan.FromSeconds(4), cp.Config.FabricOperationTimeout);
            Assert.AreEqual(TimeSpan.FromDays(2), cp.Config.ItemExpiration);

            Assert.IsTrue(classChanged);
            Assert.IsFalse(propertyChanged);
        }

        private void Cp_OnConfigurationPropertyChangedEvent(object sender, EventArgs e)
        {
            propertyChanged = true;
        }

        private void Cp_OnConfigurationClassChangedEvent(object sender, ConfigurationClassChangedEventArgs<TestQueueServiceConfiguration> e)
        {
            classChanged = true;
        }

        [TestMethod]
        public void ConfigurationProvider_ClassChangedEventArgs()
        {
            const string c_name = "TestQueueService.json";
            var tqsc = LoadConfiguration<TestQueueServiceConfiguration>(c_name);
            var args = new ConfigurationClassChangedEventArgs<TestQueueServiceConfiguration>(c_name, tqsc, tqsc);

            Assert.AreEqual(c_name, args.Name);
            Assert.AreEqual(tqsc, args.CurrentConfiguration);
            Assert.AreEqual(tqsc, args.NewConfiguration);
        }

        [TestMethod]
        public void ConfigurationProvider_ValidateInterfaces()
        {
            KeyedCollection<string, ConfigurationSection> kc = new TestKeyedCollection();
            ConfigurationProvider<TestQueueServiceConfiguration> cp = new ConfigurationProvider<TestQueueServiceConfiguration>(serviceName, configPath, kc);

            IConfigurationProvider<TestQueueServiceConfiguration> icp = cp as IConfigurationProvider<TestQueueServiceConfiguration>;
            Assert.IsNotNull(icp);
            Assert.IsInstanceOfType(icp, typeof(IConfigurationProvider<TestQueueServiceConfiguration>));
        }

        [TestMethod]
        public void ConfigurationProvider_CompareHashes()
        {
            byte[] b1 = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            byte[] b2 = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            byte[] b3 = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            byte[] b4 = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 11 };

            KeyedCollection<string, ConfigurationSection> kc = new TestKeyedCollection();
            ConfigurationProvider<TestQueueServiceConfiguration> cp = new ConfigurationProvider<TestQueueServiceConfiguration>(serviceName, configPath, kc);

            Assert.IsFalse(cp.CompareHashes(null, null));
            Assert.IsFalse(cp.CompareHashes(b1, null));
            Assert.IsFalse(cp.CompareHashes(null, b2));
            Assert.IsFalse(cp.CompareHashes(b1, b3));
            Assert.IsFalse(cp.CompareHashes(b3, b2));
            Assert.IsFalse(cp.CompareHashes(b1, b4));
            Assert.IsFalse(cp.CompareHashes(b4, b1));
            Assert.IsTrue(cp.CompareHashes(b1, b2));
            Assert.IsTrue(cp.CompareHashes(b2, b1));
        }

        [TestMethod]
        public void ConfigurationProvider_AccessMethods()
        {
            KeyedCollection<string, ConfigurationSection> kc = new TestKeyedCollection();
            ConfigurationProvider<TestQueueServiceConfiguration> cp = new ConfigurationProvider<TestQueueServiceConfiguration>(serviceName, configPath, kc);

            Assert.AreEqual("Test String", cp.GetConfigurationValue("key", "Test String"));
            Assert.AreEqual(10, cp.GetConfigurationValue("key", 10));
            Assert.AreEqual(long.MaxValue, cp.GetConfigurationValue("key", long.MaxValue));
            Assert.AreEqual(100.0, cp.GetConfigurationValue("key", 100.0));
            Assert.AreEqual(true, cp.GetConfigurationValue("key", true));
            Assert.AreEqual(DateTime.MaxValue, cp.GetConfigurationValue("key", DateTime.MaxValue));
            Assert.AreEqual(DateTimeOffset.MaxValue, cp.GetConfigurationValue("key", DateTimeOffset.MaxValue));
            Assert.AreEqual(TimeSpan.FromMinutes(30), cp.GetConfigurationValue("key", TimeSpan.FromMinutes(30)));

            string s = "Secure Value";
            SecureString ss = cp.GetEncryptedConfigurationValue("key", s);
            Assert.AreEqual(s.Length, ss.Length);
        }

        [TestMethod]
        public void ServiceFabricCommon_ConfigurationTests()
        {
            KeyedCollection<string, ConfigurationSection> kc = new TestKeyedCollection();
            ConfigurationProvider<TestQueueServiceConfiguration> cp = new ConfigurationProvider<TestQueueServiceConfiguration>(serviceName, configPath, kc);

            Assert.AreEqual<int>(50000, cp.Config.MaxQueueCapacityPerPartition);
            Assert.AreEqual<int>(15000, cp.Config.MaxLeaseCapacityPerPartition);
            Assert.AreEqual(1000, cp.Config.MaxExpiredCapacityPerPartition);
            Assert.AreEqual<double>(0.80, cp.Config.CapacityWarningPercent);
            Assert.AreEqual<double>(0.95, cp.Config.CapacityErrorPercent);
            Assert.AreEqual<int>(5, cp.Config.MaximumDequeueCount);
            Assert.AreEqual<int>(5, cp.Config.NumberOfQueues);
            Assert.AreEqual<double>(0.10, cp.Config.LeaseItemPercentWarning);
            Assert.AreEqual<double>(0.20, cp.Config.LeaseItemPercentError);
            Assert.AreEqual<TimeSpan>(TimeSpan.FromSeconds(120), cp.Config.LeaseDuration);
            Assert.AreEqual<TimeSpan>(TimeSpan.FromSeconds(30), cp.Config.HealthCheckInterval);
            Assert.AreEqual<TimeSpan>(TimeSpan.FromSeconds(30), cp.Config.LeaseCheckInterval);
            Assert.AreEqual<TimeSpan>(TimeSpan.FromSeconds(4), cp.Config.FabricOperationTimeout);
            Assert.AreEqual<TimeSpan>(TimeSpan.FromHours(48), cp.Config.ItemExpiration);

            // Cast as IQueueServiceConfiguration.
            IQueueServiceConfiguration qsc = (IQueueServiceConfiguration)cp.Config;
            Assert.IsNotNull(qsc);
            Assert.AreEqual<int>(50000, qsc.MaxQueueCapacityPerPartition);
            Assert.AreEqual<int>(15000, qsc.MaxLeaseCapacityPerPartition);
            Assert.AreEqual(1000, qsc.MaxExpiredCapacityPerPartition);
            Assert.AreEqual<double>(0.80, qsc.CapacityWarningPercent);
            Assert.AreEqual<double>(0.95, qsc.CapacityErrorPercent);
            Assert.AreEqual<int>(5, qsc.MaximumDequeueCount);
            Assert.AreEqual<int>(5, qsc.NumberOfQueues);
            Assert.AreEqual<double>(0.10, qsc.LeaseItemPercentWarning);
            Assert.AreEqual<double>(0.20, qsc.LeaseItemPercentError);
            Assert.AreEqual<TimeSpan>(TimeSpan.FromSeconds(120), qsc.LeaseDuration);
            Assert.AreEqual<TimeSpan>(TimeSpan.FromSeconds(4), qsc.FabricOperationTimeout);
            Assert.AreEqual<TimeSpan>(TimeSpan.FromDays(2), qsc.ItemExpiration);
        }

        /// <summary>
        /// Loads a configuration file from the PackageRoot\Config directory.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filename"></param>
        /// <returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        private T LoadConfiguration<T>(string filename)
        {
            string filepath = $@"{configPath}\{filename}";

            using (FileStream fs = new FileStream(filepath, FileMode.Open))
            using (StreamReader reader = new StreamReader(fs, Encoding.Default, true, 2048, true))
            {
                string json = reader.ReadToEnd();

                var serializer = new JavaScriptSerializer();
                return serializer.Deserialize<T>(json);
            }
        }
    }

    /// <summary>
    /// Test KeyedCollection class.
    /// </summary>
    internal class TestKeyedCollection : KeyedCollection<string, ConfigurationSection>
    {
        protected override string GetKeyForItem(ConfigurationSection item)
        {
            return item.Name;
        }
    }
}
