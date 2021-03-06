﻿using System.Collections.Generic;
using HA4IoT.Contracts.Hardware;

namespace HA4IoT.Contracts.Core
{
    public interface IDeviceController
    {
        void AddDevice(IDevice device);

        TDevice GetDevice<TDevice>(DeviceId id) where TDevice : IDevice;

        TDevice GetDevice<TDevice>() where TDevice : IDevice;

        IList<TDevice> GetDevices<TDevice>() where TDevice : IDevice;

        IList<IDevice> GetDevices();
    }
}
