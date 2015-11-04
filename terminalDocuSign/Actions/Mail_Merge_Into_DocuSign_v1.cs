﻿using AutoMapper;
using Data.Entities;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Newtonsoft.Json;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TerminalBase.BaseClasses;
using TerminalBase.Infrastructure;
using terminalDocuSign.DataTransferObjects;
using terminalDocuSign.Services;
using Utilities.Configuration.Azure;

namespace terminalDocuSign.Actions
{
    public class Mail_Merge_Into_DocuSign_v1 : BasePluginAction
    {
        readonly DocuSignManager _docuSignManager;
        string _dataSourceValue;
        string _docuSignTemplateValue;

        public Mail_Merge_Into_DocuSign_v1() : base()
        {
            _docuSignManager = new DocuSignManager();
        }

        /// <summary>
        /// Action processing infrastructure.
        /// </summary>
        public async Task<PayloadDTO> Run(ActionDO curActionDO)
        {
            return null;
        }

        /// <summary>
        /// Create configuration controls crate.
        /// </summary>
        private async Task<CrateDTO> CreateConfigurationControlsCrate()
        {
            var controlList = new List<ControlDefinitionDTO>();

            controlList.Add(new DropDownListControlDefinitionDTO()
            {
                Label = "1. Where is your Source Data?",
                Name = "DataSource",
                ListItems = await GetDataSourceListItems("Table Data Generator")
            });

            controlList.Add(_docuSignManager.CreateDocuSignTemplatePicker(false, "DocuSignTemplate", "2. Use which DocuSign Template?"));
            controlList.Add(new ButtonControlDefinitionDTO()
            {
                Label = "Continue",
                Name = "Continue"
            });

            return PackControlsCrate(controlList.ToArray());
        }

        private async Task<List<ListItem>> GetDataSourceListItems(string tag)
        {
            var httpClient = new HttpClient();
            var url = CloudConfigurationManager.GetSetting("CoreWebServerUrl")
            + "route_nodes/available/?tag=" + tag;

            using (var response = await httpClient.GetAsync(url).ConfigureAwait(false))
            {
                var content = await response.Content.ReadAsStringAsync();
                var activityTemplate = JsonConvert.DeserializeObject<List<ActivityTemplateDTO>>(content);
                return activityTemplate.Select(at => new ListItem() { Key = at.Label, Value = at.Name }).ToList();
            }
        }

        /// <summary>
        /// Looks for upstream and downstream Creates.
        /// </summary>
        protected override async Task<ActionDO> InitialConfigurationResponse(ActionDO curActionDO)
        {
            CrateStorageDTO crateStrorageDTO;
            if (curActionDO.CrateStorage == null)
            {
                curActionDO.UpdateCrateStorageDTO(new CrateStorageDTO().CrateDTO);
            }

            if (curActionDO.Id > 0)
            {
                if (curActionDO.AuthToken == null || curActionDO.AuthToken.Token == null)
                {
                    CrateDTO configurationControlsCrate = await CreateNoAuthCrate();
                    crateStrorageDTO = AssembleCrateStorage(configurationControlsCrate);
                }
                else
                {
                    var docuSignAuthDTO = JsonConvert.DeserializeObject<DocuSignAuthDTO>(curActionDO.AuthToken.Token);

                    //build a controls crate to render the pane
                    CrateDTO configurationControlsCrate = await CreateConfigurationControlsCrate();
                    CrateDTO templatesFieldCrate = _docuSignManager.PackCrate_DocuSignTemplateNames(docuSignAuthDTO);
                    crateStrorageDTO = AssembleCrateStorage(templatesFieldCrate, configurationControlsCrate);
                }
                curActionDO.UpdateCrateStorageDTO(crateStrorageDTO.CrateDTO);
            }
            else
            {
                throw new ArgumentException(
                    "Configuration requires the submission of an Action that has a real ActionId");
            }
            return curActionDO;
        }

        private Task<CrateDTO> CreateNoAuthCrate()
        {
            var controlList = new List<ControlDefinitionDTO>();

            controlList.Add(new TextBlockControlDefinitionDTO()
            {
                Value = "This action requires authentication. Please authenticate."
            });
            return Task.FromResult(PackControlsCrate(controlList.ToArray()));
        }

        /// <summary>
        /// If there's a value in select_file field of the crate, then it is a followup call.
        /// </summary>
        public override ConfigurationRequestType ConfigurationEvaluator(ActionDO curActionDO)
        {
            // Do not tarsnfer to follow up when child actions are already present 
            if (curActionDO.ChildrenActions.Count() > 0) return ConfigurationRequestType.Initial;

            // "Follow up" phase is when Continue button is clicked 
            ButtonControlDefinitionDTO button = GetStdConfigurationControl<ButtonControlDefinitionDTO>(
                curActionDO.CrateStorageDTO().CrateDTO, "Continue");
            if (button == null) return ConfigurationRequestType.Initial;
            if (button.Clicked == false) return ConfigurationRequestType.Initial;

            // If no values selected in textboxes, remain on initial phase
            DropDownListControlDefinitionDTO dataSource = GetStdConfigurationControl<DropDownListControlDefinitionDTO>(
                curActionDO.CrateStorageDTO().CrateDTO, "DataSource");
            if (dataSource.Value == null) return ConfigurationRequestType.Initial;
            _dataSourceValue = dataSource.Value;

            DropDownListControlDefinitionDTO docuSignTemplate = GetStdConfigurationControl<DropDownListControlDefinitionDTO>(
                curActionDO.CrateStorageDTO().CrateDTO, "DocuSignTemplate");
            if (docuSignTemplate.Value == null) return ConfigurationRequestType.Initial;
            _docuSignTemplateValue = docuSignTemplate.Value;

            return ConfigurationRequestType.Followup;
        }

        //if the user provides a file name, this action attempts to load the excel file and extracts the column headers from the first sheet in the file.
        protected override async Task<ActionDO> FollowupConfigurationResponse(ActionDO curActionDO)
        {
            var docuSignAuthDTO = JsonConvert.DeserializeObject<DocuSignAuthDTO>(curActionDO.AuthToken.Token);
            _docuSignManager.ExtractFieldsAndAddToCrate(_docuSignTemplateValue, docuSignAuthDTO, curActionDO);

            try
            {
                using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
                {
                    ActivityTemplateDO dataSourceActTempl = uow.ActivityTemplateRepository.GetAll().FirstOrDefault(at => at.Name == _dataSourceValue);
                    if (dataSourceActTempl == null) return curActionDO;
                    curActionDO.ChildNodes.Add(new ActionDO()
                    {
                        ActivityTemplate = dataSourceActTempl,
                        IsTempId = true,
                        Name = dataSourceActTempl.Name,
                        Label = dataSourceActTempl.Label,
                        CrateStorage = JsonConvert.SerializeObject(new CrateStorageDTO()),
                        ParentRouteNode = curActionDO,
                        Ordering = 1
                    });

                    ActivityTemplateDO mapFieldActTempl = uow.ActivityTemplateRepository.GetAll().FirstOrDefault(at => at.Name == "MapFields");
                    if (mapFieldActTempl == null) return curActionDO;

                    curActionDO.ChildNodes.Add(new ActionDO()
                    {
                        ActivityTemplate = mapFieldActTempl,
                        IsTempId = true,
                        Name = mapFieldActTempl.Name,
                        Label = mapFieldActTempl.Label,
                        CrateStorage = JsonConvert.SerializeObject(new CrateStorageDTO()),
                        ParentRouteNode = curActionDO,
                        Ordering = 2
                    });

                    ActivityTemplateDO sendDocuSignEnvActTempl = uow.ActivityTemplateRepository.GetAll().FirstOrDefault(at => at.Name == "Send_DocuSign_Envelope");
                    if (mapFieldActTempl == null) return curActionDO;
                    curActionDO.ChildNodes.Add(new ActionDO()
                    {
                        ActivityTemplate = sendDocuSignEnvActTempl,
                        IsTempId = true,
                        Name = sendDocuSignEnvActTempl.Name,
                        CrateStorage = JsonConvert.SerializeObject(new CrateStorageDTO()),
                        Label = sendDocuSignEnvActTempl.Label,
                        ParentRouteNode = curActionDO, 
                        Ordering = 3
                    });

                    //uow.ActionRepository.Add(curActionDO);
                    //uow.Db.Entry<ActionDO>(curActionDO).State = System.Data.Entity.EntityState.Modified;
                    //uow.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                return null;
            }


            return await Task.FromResult(curActionDO);
        }
    }
}