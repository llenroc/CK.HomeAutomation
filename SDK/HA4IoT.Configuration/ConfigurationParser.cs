﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Windows.Storage;
using HA4IoT.Contracts.Areas;
using HA4IoT.Contracts.Components;
using HA4IoT.Contracts.Configuration;
using HA4IoT.Contracts.Core;
using HA4IoT.Contracts.Hardware;
using HA4IoT.Contracts.Logging;
using HA4IoT.Contracts.Sensors;
using HA4IoT.Contracts.Services;
using HA4IoT.Core;

namespace HA4IoT.Configuration
{
    public class ConfigurationParser
    {
        private readonly Dictionary<string, IConfigurationExtender> _configurationExtenders = new Dictionary<string, IConfigurationExtender>();
        private readonly IController _controller;

        private XDocument _configuration;

        public ConfigurationParser(IController controller)
        {
            if (controller == null) throw new ArgumentNullException(nameof(controller));

            _controller = controller;
        }

        public void RegisterConfigurationExtender(IConfigurationExtender configurationExtender)
        {
            if (configurationExtender == null) throw new ArgumentNullException(nameof(configurationExtender));

            _configurationExtenders.Add(configurationExtender.Namespace, configurationExtender);
        }

        public void ParseConfiguration(XDocument configuration)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));

            _configuration = configuration;

            ParseServices();
            ParseDevices();
            ParseAreas();

            TriggerOnConfigurationParsed();
        }

        public void ParseConfiguration()
        {
            XDocument configuration = LoadConfiguration();
            if (configuration == null)
            {
                return;
            }

            ParseConfiguration(configuration);
        }

        public IBinaryInput ParseBinaryInput(XElement element)
        {
            return GetConfigurationExtender(element).ParseBinaryInput(element).WithInvertedState(element.GetBoolFromAttribute("invertState", false));
        }

        public IBinaryOutput ParseBinaryOutput(XElement element)
        {
            return GetConfigurationExtender(element).ParseBinaryOutput(element).WithInvertedState(element.GetBoolFromAttribute("invertState", false));
        }

        public INumericValueSensorEndpoint ParseNumericValueSensor(XElement element)
        {
            return GetConfigurationExtender(element).ParseNumericValueSensor(element);
        }

        private XDocument LoadConfiguration()
        {
            string filename = Path.Combine(ApplicationData.Current.LocalFolder.Path, "Configuration.xml");
            if (!File.Exists(filename))
            {
                Log.Info($"Skipped loading XML configuration because file '{filename}' does not exist.");
                return null;
            }

            using (var fileStream = File.OpenRead(filename))
            {
                return XDocument.Load(fileStream);
            }
        }

        private void ParseServices()
        {
            var devicesElement = _configuration.Root.Element("Services");
            foreach (XElement serviceElement in devicesElement.Elements())
            {
                try
                {
                    IService service = GetConfigurationExtender(serviceElement).ParseService(serviceElement);
                    _controller.RegisterService(service);
                }
                catch (Exception exception)
                {
                    Log.Warning(exception, $"Unable to parse service node '{serviceElement.Name}'.");
                }
            }
        }

        private void ParseDevices()
        {
            var devicesElement = _configuration.Root.Element("Devices");
            foreach (XElement deviceElement in devicesElement.Elements())
            {
                try
                {
                    IDevice device = GetConfigurationExtender(deviceElement).ParseDevice(deviceElement);
                    _controller.AddDevice(device);
                }
                catch (Exception exception)
                {
                    Log.Warning(exception, $"Unable to parse device node '{deviceElement.Name}'.");
                }
            }
        }

        private void ParseAreas()
        {
            var roomsElement = _configuration.Root.Element("Areas");
            foreach (XElement areaElement in roomsElement.Elements())
            {
                try
                {
                    _controller.AddArea(ParseArea(areaElement));
                }
                catch (Exception exception)
                {
                    Log.Warning(exception, $"Unable to parse area node '{areaElement.Name}'.");
                }
            }
        }

        private IArea ParseArea(XElement roomElement)
        {
            var area = new Area(new AreaId(roomElement.GetMandatoryStringFromAttribute("id")), _controller);

            foreach (var componentElement in roomElement.Element("Components").Elements())
            {
                try
                {
                    IComponent component = GetConfigurationExtender(componentElement).ParseComponent(componentElement);
                    area.AddComponent(component);
                }
                catch (Exception exception)
                {
                    Log.Warning(exception, $"Unable to parse component node '{componentElement.Name}'.");
                }
            }

            return area;
        }

        private IConfigurationExtender GetConfigurationExtender(XElement element)
        {
            IConfigurationExtender extender;
            if (!_configurationExtenders.TryGetValue(element.Name.NamespaceName, out extender))
            {
                throw new InvalidOperationException("No configuration extender found for element with namespace '" + element.Name.NamespaceName + "'.");
            }

            return extender;
        }

        private void TriggerOnConfigurationParsed()
        {
            foreach (var configurationExtender in _configurationExtenders.Values)
            {
                configurationExtender.OnConfigurationParsed();
            }
        }
    }
}
