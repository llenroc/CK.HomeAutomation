﻿using FluentAssertions;
using HA4IoT.Contracts.Actuators;
using HA4IoT.Tests.Mockups;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;

namespace HA4IoT.Automations.Tests
{
    [TestClass]
    public class ConditionalOnAutomationTests
    {
        [TestMethod]
        public void Empty_ConditionalOnAutomation()
        {
            var testController = new TestController();
            var automation = new ConditionalOnAutomation(AutomationIdFactory.EmptyId, testController.Timer);

            var testButtonFactory = new TestButtonFactory(testController.Timer);
            var testStateMachineFactory = new TestStateMachineFactory();

            var testButton = testButtonFactory.CreateTestButton();
            var testOutput = testStateMachineFactory.CreateTestStateMachineWithOnOffStates();

            automation.WithTrigger(testButton.GetPressedShortlyTrigger());
            automation.WithActuator(testOutput);
            
            testOutput.GetState().ShouldBeEquivalentTo(BinaryStateId.Off);
            testButton.PressShortly();
            testOutput.GetState().ShouldBeEquivalentTo(BinaryStateId.On);
        }
    }
}
