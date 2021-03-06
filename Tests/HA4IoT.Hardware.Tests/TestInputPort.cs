﻿using HA4IoT.Contracts.Hardware;

namespace HA4IoT.Hardware.Tests
{
    public class TestInputPort : TestPort, IBinaryInput
    {
        public new IBinaryInput WithInvertedState(bool value = true)
        {
            return (IBinaryInput) base.WithInvertedState();
        }
    }
}
