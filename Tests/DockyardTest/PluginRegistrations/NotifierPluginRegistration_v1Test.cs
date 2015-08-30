﻿using System;
using System.Collections.Generic;
using System.Linq;
using Core.PluginRegistrations;
using Data.Entities;
using Newtonsoft.Json;
using NUnit.Framework;
using UtilitiesTesting;
using UtilitiesTesting.Fixtures;

namespace DockyardTest.PluginRegistrations
{
    [TestFixture]
    [Category("NotifierPluginRegistration_v1")]
    public class NotifierPluginRegistration_v1Test : BaseTest
    {
        private NotifierPluginRegistration_v1 _notifierPluginRegistration_v1;
#region configuration setting Json
        private const string emailAction = @"{""FieldDefinitions"":[{""Name"":""Email Address"",""Required"":""true"",""Value"":"""",""FieldLabel"":""""},{""Name"":""Friendly Name"",""Required"":""true"",""Value"":"""",""FieldLabel"":""""},{""Name"":""Subject"",""Required"":""true"",""Value"":"""",""FieldLabel"":""""},{""Name"":""Body"",""Required"":""true"",""Value"":"""",""FieldLabel"":""""}]}";
        private const string textMessageAction = @"{""FieldDefinitions"":[{""Name"":""Phone Number"",""Required"":""true"",""Value"":"""",""FieldLabel"":""""},{""Name"":""Message"",""Required"":""true"",""Value"":"""",""FieldLabel"":""""}]}";
#endregion

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            _notifierPluginRegistration_v1 = new NotifierPluginRegistration_v1();
        }

        [Test]
        public void CanGetAvailableActions()
        {
            Assert.AreEqual(_notifierPluginRegistration_v1.AvailableActions.Count(), 2);
            Assert.AreEqual(((List<ActionTemplateDO>)_notifierPluginRegistration_v1.AvailableActions)[0].ActionType, "Send an Email");
            Assert.AreEqual(((List<ActionTemplateDO>)_notifierPluginRegistration_v1.AvailableActions)[1].ActionType, "Send a Text (SMS) Message");
        }

        [Test]
        public void CanGetConfigurationSettings()
        {
            ActionDO curActionForEmail = FixtureData.TestAction4();
            ActionDO curActionForMessage = FixtureData.TestAction5();
            string resultJsonEmail = JsonConvert.SerializeObject(_notifierPluginRegistration_v1.GetConfigurationSettings(curActionForEmail));
            string resultJsonMessage = JsonConvert.SerializeObject(_notifierPluginRegistration_v1.GetConfigurationSettings(curActionForMessage));
            Assert.AreEqual(resultJsonEmail, emailAction);
            Assert.AreEqual(resultJsonMessage, textMessageAction);
        }

        [Test]
        [ExpectedException(ExpectedException = typeof(NullReferenceException))]
        public void GetConfigurationSettings_CheckForAcitonIsNullOrEmpy()
        {
            ActionDO curActionUserLableEmpty = FixtureData.TestAction6();
            _notifierPluginRegistration_v1.GetConfigurationSettings(curActionUserLableEmpty);
        }

        [Test]
        public void GetFieldMappingTargets_IsNull()
        {
            Assert.IsNull(_notifierPluginRegistration_v1.GetFieldMappingTargets(string.Empty, string.Empty));
        }
    }
}