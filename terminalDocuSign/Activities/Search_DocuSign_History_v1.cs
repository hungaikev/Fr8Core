﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fr8.Infrastructure.Data.Constants;
using Fr8.Infrastructure.Data.Control;
using Fr8.Infrastructure.Data.Crates;
using Fr8.Infrastructure.Data.DataTransferObjects;
using Fr8.Infrastructure.Data.Managers;
using Fr8.Infrastructure.Data.Manifests;
using Fr8.Infrastructure.Data.States;
using Fr8.TerminalBase.Models;
using Newtonsoft.Json;
using terminalDocuSign.DataTransferObjects;
using terminalDocuSign.Services.New_Api;

namespace terminalDocuSign.Activities
{
    public class Search_DocuSign_History_v1  : BaseDocuSignActivity
    {
        public static ActivityTemplateDTO ActivityTemplateDTO = new ActivityTemplateDTO
        {
            Name = "Search_DocuSign_History",
            Label = "Search DocuSign History",
            Version = "1",
            Category = ActivityCategory.Receivers,
            NeedsAuthentication = true,
            MinPaneWidth = 380,
            Tags = Tags.Internal,
            WebService = TerminalData.WebServiceDTO,
            Terminal = TerminalData.TerminalDTO
        };
        protected override ActivityTemplateDTO MyTemplate => ActivityTemplateDTO;

        internal class ActivityUi : StandardConfigurationControlsCM
        {
            [JsonIgnore]
            public TextBox SearchText { get; set; }
            
            [JsonIgnore]
            public DropDownList Folder { get; set; }

            [JsonIgnore]
            public DropDownList Status { get; set; }

            public ActivityUi()
            {
                Controls = new List<ControlDefinitionDTO>();

                Controls.Add(new TextArea
                {
                    IsReadOnly = true,
                    Label = "",
                    Value = "<p>Search for DocuSign Envelopes where the following are true:</p>" +
                            "<div>Envelope contains text:</div>"
                });

                Controls.Add(SearchText = new TextBox
                {
                    Name = "SearchText",
                    Events = new List<ControlEvent> {ControlEvent.RequestConfig},
                                          });

                Controls.Add(Folder = new DropDownList
                {
                    Label = "Envelope is in folder:",
                    Name = "Folder",
                    Events = new List<ControlEvent> {ControlEvent.RequestConfig},
                    Source = null
                                      });

                Controls.Add(Status = new DropDownList
                {
                    Label = "Envelope has status:",
                    Name = "Status",
                    Events = new List<ControlEvent> {ControlEvent.RequestConfig},
                    Source = null
                                      });

                Controls.Add(new RunPlanButton());
            }
        }


        protected override string ActivityUserFriendlyName => "Search DocuSign History";

        public Search_DocuSign_History_v1(ICrateManager crateManager, IDocuSignManager docuSignManager) 
            : base(crateManager, docuSignManager)
        {
        }

        protected override async Task RunDS()
        {
            Success();
        }
        
        protected override async Task InitializeDS()
        {
            var actionUi = new ActivityUi();
            var docuSignAuthDTO = JsonConvert.DeserializeObject<DocuSignAuthTokenDTO>(AuthorizationToken.Token);           
            var configurationCrate = PackControls(actionUi);
            //commented out by FR-2400
            //_docuSignManager.FillFolderSource(configurationCrate, "Folder", docuSignAuthDTO);
            //_docuSignManager.FillStatusSource(configurationCrate, "Status");
            Storage.Add(configurationCrate);
            await ConfigureNestedActivities(actionUi);
        }

        protected override async Task FollowUpDS()
        {
            if (ConfigurationControls == null)
            {
                return;
            }
            var actionUi = new ActivityUi();
            actionUi.ClonePropertiesFrom(ConfigurationControls);
            await ConfigureNestedActivities(actionUi);
        }

        private async Task ConfigureNestedActivities(ActivityUi actionUi)
        {
            var config = new Query_DocuSign_v1.ActivityUi
            {
                Folder = {Value = actionUi.Folder.Value}, 
                Status = {Value = actionUi.Status.Value}, 
                SearchText = {Value = actionUi.SearchText.Value}
            };
            
            var template = (await FindTemplates(x => x.Name == "Query_DocuSign")).FirstOrDefault();

            if (template == null)
            {
                throw new Exception("Can't find activity template: Query_DocuSign");
            }

            var storage = new CrateStorage(Crate.FromContent("Config", config))
            {
                PackControlsCrate(new TextArea
                {
                    IsReadOnly = true,
                    Label = "",
                    Value = "<p>This activity is managed by the parent activity</p>"
                })
            };
            var activity = ActivityPayload.ChildrenActivities.OfType<ActivityPayload>().FirstOrDefault();

            if (activity == null)
            {
                activity = new ActivityPayload
                {
                    ActivityTemplate = template,
                    Name = template.Label,
                    Ordering = 1,
                };

                ActivityPayload.ChildrenActivities.Add(activity);
            }
            activity.CrateStorage = storage;

        }

        private async Task<IEnumerable<ActivityTemplateDTO>> FindTemplates(Predicate<ActivityTemplateDTO> query)
        {
            var templates = await HubCommunicator.GetActivityTemplates(true);
            return templates.Where(x => query(x));
        }
    }
}