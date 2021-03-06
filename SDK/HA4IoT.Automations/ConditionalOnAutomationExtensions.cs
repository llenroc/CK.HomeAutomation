﻿using System;
using HA4IoT.Contracts.Areas;

namespace HA4IoT.Automations
{
    public static class ConditionalOnAutomationExtensions
    {
        public static ConditionalOnAutomation SetupConditionalOnAutomation(this IArea area)
        {
            if (area == null) throw new ArgumentNullException(nameof(area));

            var automation =
                new ConditionalOnAutomation(
                    AutomationIdFactory.CreateIdFrom<ConditionalOnAutomation>(area), 
                    area.Controller.Timer);

            area.AddAutomation(automation);

            return automation;
        }
    }
}
