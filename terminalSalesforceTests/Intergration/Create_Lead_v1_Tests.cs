﻿using System.Linq;
using System.Threading.Tasks;
using Data.Control;
using Data.Crates;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using HealthMonitor.Utility;
using Hub.Managers;
using Hub.Managers.APIManagers.Transmitters.Restful;
using NUnit.Framework;
using terminalSalesforceTests.Fixtures;

namespace terminalSalesforceTests.Intergration
{
    [Explicit]
    public class Create_Lead_v1_Tests : BaseHealthMonitorTest
    {
        public override string TerminalName
        {
            get { return "terminalSalesforce"; }
        }

        [Test, Category("intergration.terminalSalesforce")]
        public async void Create_Lead_Initial_Configuration_Check_Crate_Structure()
        {
            //Act
            var initialConfigActionDto = await PerformInitialConfiguration();

            //Assert
            Assert.IsNotNull(initialConfigActionDto.CrateStorage,
                "Initial Configuration of Create Lead activity contains no crate storage");

            AssertConfigurationControls(Crate.FromDto(initialConfigActionDto.CrateStorage));
        }

        [Test, Category("intergration.terminalSalesforce")]
        [ExpectedException(
            ExpectedException = typeof(RestfulServiceException)
        )]
        public async void Create_Lead_Initial_Configuration_Without_AuthToken_Exception_Thrown()
        {
            //Arrange
            string terminalConfigureUrl = GetTerminalConfigureUrl();

            //prepare the create lead action DTO
            var requestActionDTO = HealthMonitor_FixtureData.Create_Lead_v1_InitialConfiguration_ActionDTO();
            requestActionDTO.AuthToken = null;

            //Act
            //perform post request to terminal and return the result
            await HttpPostAsync<ActivityDTO, ActivityDTO>(terminalConfigureUrl, requestActionDTO);
        }

        [Test, Category("intergration.terminalSalesforce")]
        public async void Create_Lead_Run_With_NoAuth_Check_NoAuthProvided_Error()
        {
            //Arrange
            var initialConfigActionDto = await PerformInitialConfiguration();
            initialConfigActionDto = SetLastName(initialConfigActionDto);
            initialConfigActionDto = SetCompanyName(initialConfigActionDto);
            AddOperationalStateCrate(initialConfigActionDto, new OperationalStateCM());

            //Act
            var responseOperationalState = await HttpPostAsync<ActivityDTO, PayloadDTO>(GetTerminalRunUrl(), initialConfigActionDto);

            //Assert
            Assert.IsNotNull(responseOperationalState);
            var curOperationalState =
                Crate.FromDto(responseOperationalState.CrateStorage).CratesOfType<OperationalStateCM>().Single().Content;
            Assert.AreEqual("No AuthToken provided.", curOperationalState.CurrentActivityErrorMessage, "Authentication is mishandled at activity side.");
        }

        [Test, Category("intergration.terminalSalesforce")]
        public async void Create_Lead_Run_With_NoLastName_Check_NoLastNameProvided_Error()
        {
            //Arrange
            var initialConfigActionDto = await PerformInitialConfiguration();
            initialConfigActionDto = SetCompanyName(initialConfigActionDto);
            initialConfigActionDto.AuthToken = HealthMonitor_FixtureData.Salesforce_AuthToken();
            AddOperationalStateCrate(initialConfigActionDto, new OperationalStateCM());

            //Act
            var responseOperationalState = await HttpPostAsync<ActivityDTO, PayloadDTO>(GetTerminalRunUrl(), initialConfigActionDto);

            //Assert
            Assert.IsNotNull(responseOperationalState);
            var curOperationalState =
                Crate.FromDto(responseOperationalState.CrateStorage).CratesOfType<OperationalStateCM>().Single().Content;
            Assert.AreEqual("No last name found in activity.", curOperationalState.CurrentActivityErrorMessage, "Action works without last name");
        }

        [Test, Category("intergration.terminalSalesforce")]
        public async void Create_Lead_Run_With_NoCompanyName_Check_NoCompanyNameProvided_Error()
        {
            //Arrange
            var initialConfigActionDto = await PerformInitialConfiguration();
            initialConfigActionDto = SetLastName(initialConfigActionDto);
            initialConfigActionDto.AuthToken = HealthMonitor_FixtureData.Salesforce_AuthToken();
            AddOperationalStateCrate(initialConfigActionDto, new OperationalStateCM());

            //Act
            var responseOperationalState = await HttpPostAsync<ActivityDTO, PayloadDTO>(GetTerminalRunUrl(), initialConfigActionDto);

            //Assert
            Assert.IsNotNull(responseOperationalState);
            var curOperationalState =
                Crate.FromDto(responseOperationalState.CrateStorage).CratesOfType<OperationalStateCM>().Single().Content;
            Assert.AreEqual("No company name found in activity.", curOperationalState.CurrentActivityErrorMessage, "Action works without company name");
        }

        [Test, Category("intergration.terminalSalesforce")]
        public async void Create_Contact_Run_With_ValidParameter_Check_PayloadDto_OperationalState()
        {
            //Arrange
            var initialConfigActionDto = await PerformInitialConfiguration();
            initialConfigActionDto = SetLastName(initialConfigActionDto);
            initialConfigActionDto = SetCompanyName(initialConfigActionDto);
            initialConfigActionDto.AuthToken = HealthMonitor_FixtureData.Salesforce_AuthToken();
            AddOperationalStateCrate(initialConfigActionDto, new OperationalStateCM());

            //Act
            var responseOperationalState = await HttpPostAsync<ActivityDTO, PayloadDTO>(GetTerminalRunUrl(), initialConfigActionDto);

            //Assert
            Assert.IsNotNull(responseOperationalState);
        }

        /// <summary>
        /// Performs Initial Configuration request of Create Contact action
        /// </summary>
        private async Task<ActivityDTO> PerformInitialConfiguration()
        {
            //get the terminal configure URL
            string terminalConfigureUrl = GetTerminalConfigureUrl();

            //prepare the create account action DTO
            var requestActionDTO = HealthMonitor_FixtureData.Create_Lead_v1_InitialConfiguration_ActionDTO();

            //perform post request to terminal and return the result
            var resultActionDto = await HttpPostAsync<ActivityDTO, ActivityDTO>(terminalConfigureUrl, requestActionDTO);

            using (var updater = Crate.UpdateStorage(resultActionDto))
            {
                var controls = updater.CrateStorage.CrateContentsOfType<StandardConfigurationControlsCM>().Single();
                controls.Controls.OfType<TextSource>().ToList().ForEach(ctl => ctl.ValueSource = "specific");
            }

            return resultActionDto;
        }

        private void AssertConfigurationControls(CrateStorage curActionCrateStorage)
        {
            var configurationControls = curActionCrateStorage.CratesOfType<StandardConfigurationControlsCM>().Single();

            Assert.AreEqual(3, configurationControls.Content.Controls.Count,
                "Create Lead does not contain the required 3 fields.");

            Assert.IsTrue(configurationControls.Content.Controls.Any(ctrl => ctrl.Name.Equals("firstName")),
                "Create Lead activity does not have First Name control");

            Assert.IsTrue(configurationControls.Content.Controls.Any(ctrl => ctrl.Name.Equals("lastName")),
                "Create Lead does not have Last Name control");

            Assert.IsTrue(configurationControls.Content.Controls.Any(ctrl => ctrl.Name.Equals("companyName")),
                "Create Lead does not have Company Name control");

            //@AlexAvrutin: Commented this since the textboxes here do not require requestConfig event. 
            //Assert.IsFalse(configurationControls.Content.Controls.Any(ctrl => ctrl.Events.Count != 1),
            //    "Create Lead controls are not subscribed to on Change events");
        }

        private ActivityDTO SetLastName(ActivityDTO curActivityDto)
        {
            using (var updater = Crate.UpdateStorage(curActivityDto))
            {
                var controls = updater.CrateStorage.CrateContentsOfType<StandardConfigurationControlsCM>().Single();

                var targetUrlTextBox = (TextSource)controls.Controls[1];
                targetUrlTextBox.ValueSource = "specific";
                targetUrlTextBox.TextValue = "IntegrationTestLastName";
            }

            return curActivityDto;
        }

        private ActivityDTO SetCompanyName(ActivityDTO curActivityDto)
        {
            using (var updater = Crate.UpdateStorage(curActivityDto))
            {
                var controls = updater.CrateStorage.CrateContentsOfType<StandardConfigurationControlsCM>().Single();

                var targetUrlTextBox = (TextSource)controls.Controls[2];
                targetUrlTextBox.ValueSource = "specific";
                targetUrlTextBox.TextValue = "IntegrationTestCompanyName";
            }

            return curActivityDto;
        }
    }
}
