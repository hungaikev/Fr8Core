﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Core.Managers;
using Data.Interfaces;
using Core.Services;
using Data.Entities;
using Data.States;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;
using NUnit.Framework;
using StructureMap;
using Utilities;
using UtilitiesTesting;
using UtilitiesTesting.Fixtures;
using Microsoft.SqlServer.Management.Common;
using Newtonsoft.Json;
using NUnit.Core;
using Server = Microsoft.SqlServer.Management.Smo.Server;

namespace DockyardTest.Integration
{
    [TestFixture]
    public class RunTimeTests : BaseTest
    {

        [Test,Ignore]
        [Category("IntegrationTests")]
        public async void ITest_CanProcessHealthDemo()
        {
            string email;
            string id;
            // SETUP
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                //create a registered account
                Account _account = new Account();              
                var registeredAccount = _account.Register(uow, "devtester", "dev", "tester", "password", "User"); 
                uow.UserRepository.Add(registeredAccount);
                uow.SaveChanges();

                //create a process template linked to that account
                var healthProcessTemplate = CreateProcessTemplate_healthdemo(uow, registeredAccount);
                string healthPayloadPath = "DockyardTest\\Content\\DocusignXmlPayload_healthdemo.xml";

                var xmlPayloadFullPath = FixtureData.FindXmlPayloadFullPath(Environment.CurrentDirectory, healthPayloadPath);
                DocuSignNotification _docuSignNotification = ObjectFactory.GetInstance<DocuSignNotification>();
                _docuSignNotification.Process(registeredAccount.Id, File.ReadAllText(xmlPayloadFullPath));


            }

            // EXECUTE


            // VERIFY
            //Assert.AreEqual(id, userId);
            //Assert.IsTrue(result.Succeeded, string.Join(", ", result.Errors));

        }

        public ProcessTemplateDO CreateProcessTemplate_healthdemo(IUnitOfWork uow, DockyardAccountDO registeredAccount)
        {
            var healthProcessTemplate = FixtureData.TestProcessTemplateHealthDemo();
            healthProcessTemplate.DockyardAccount = registeredAccount;
            uow.ProcessTemplateRepository.Add(healthProcessTemplate);

            //add processnode to process
            var healthProcessNodeTemplateDO = FixtureData.TestProcessNodeTemplateHealthDemo();
            healthProcessNodeTemplateDO.ParentTemplateId = healthProcessTemplate.Id;
            uow.ProcessNodeTemplateRepository.Add(healthProcessNodeTemplateDO);

            //add criteria to processnode
            var healthCriteria = FixtureData.TestCriteriaHealthDemo();
            healthCriteria.ProcessNodeTemplateID = healthProcessNodeTemplateDO.Id;
            uow.CriteriaRepository.Add(healthCriteria);

            //add actionlist to processnode
            var healthActionList = FixtureData.TestActionListHealth1();
            healthActionList.ProcessNodeTemplateID = healthProcessNodeTemplateDO.Id;
            uow.ActionListRepository.Add(healthActionList);

            //add write action to actionlist
            var healthWriteAction = FixtureData.TestActionWriteSqlServer1();
            healthWriteAction.ActionListId = healthActionList.Id;

            //add field mappings to write action
            var health_FieldMappings = FixtureData.TestFieldMappingSettingsDTO_Health();
            healthWriteAction.FieldMappingSettings = health_FieldMappings.Serialize();

            //add configuration settings to write action
            var configuration_settings = FixtureData.TestConfigurationSettings_healthdemo();
            healthWriteAction.ConfigurationSettings = JsonConvert.SerializeObject(configuration_settings);
            uow.ActionRepository.Add(healthWriteAction);

            //add a subscription to a specific template on the docusign platform
            var health_ExternalEventSubscription = FixtureData.TestExternalEventSubscription_medical_form_v1();
            health_ExternalEventSubscription.ProcessTemplate = healthProcessTemplate;
            uow.ExternalEventRegistrationRepository.Add(health_ExternalEventSubscription);

            uow.SaveChanges();
            return healthProcessTemplate;
        }


        [Test, Ignore]
        public void ITest_CanProcessHealthDemoForm()
        {
            //Arrange 


            //Act
           
            //Assert
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var fact = uow.FactRepository.GetAll().Where(f => f.Activity == "Received").SingleOrDefault();
                Assert.IsNotNull(fact);
            }
        }
    }
}


//ServerConnection serverConnection = new ServerConnection(connection);
//Server server = new Server(serverConnection);

//// after this line, the default database will be switched to Master
//Database database = server.Databases["demodb_health"];

//// you can still use this database object and server connection to 
//// do certain things against this database, like adding database roles 
//// and users      



//// if you want to execute a script against this database, you have to open 
//// another connection and re-initiliaze the server object
//server.ConnectionContext.Disconnect();

//                connection = new SqlConnection(connectionString);
//serverConnection = new ServerConnection(connection);
//server = new Server(serverConnection);
//server.ConnectionContext.ExecuteNonQuery("CREATE TABLE New (NewId int)");
         