﻿using System;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using HA4IoT.Contracts.Actuators;
using HA4IoT.Contracts.Hardware;

namespace HA4IoT.Controller.Local
{
    public class CheckBoxBinaryStateEndpoint : IBinaryStateEndpoint
    {
        private readonly CheckBox _checkBox;

        public CheckBoxBinaryStateEndpoint(CheckBox checkBox)
        {
            if (checkBox == null) throw new ArgumentNullException(nameof(checkBox));

            _checkBox = checkBox;
        }

        public async void TurnOn(params IHardwareParameter[] parameters)
        {
            await _checkBox.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    _checkBox.IsChecked = true;
                });
        }

        public async void TurnOff(params IHardwareParameter[] parameters)
        {
            await _checkBox.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    _checkBox.IsChecked = false;
                });
        }
    }
}
