﻿using System.Globalization;
using AutoMapper;
using Data.Constants;
using Data.Entities;
using Data.Infrastructure;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Castle.DynamicProxy.Generators;
using System.Web.Routing;
using Data.Control;
using Data.Crates;
using Data.Repositories.Plan;
using Data.Infrastructure.StructureMap;
using Data.States;
using Hub.Interfaces;
using Hub.Managers;
using Hub.Managers.APIManagers.Transmitters.Restful;
using Hub.Managers.APIManagers.Transmitters.Terminal;
using Microsoft.ApplicationInsights;

namespace Hub.Services
{
    public class Activity : IActivity
    {
        private readonly ICrateManager _crate;
        private readonly IAuthorization _authorizationToken;
        private readonly TelemetryClient _telemetryClient;
        private readonly ISecurityServices _security;
        private readonly IActivityTemplate _activityTemplate;
        private readonly IRouteNode _routeNode;

        public Activity()
        {
            _activityTemplate = ObjectFactory.GetInstance<IActivityTemplate>();
            _authorizationToken = ObjectFactory.GetInstance<IAuthorization>();
            _routeNode = ObjectFactory.GetInstance<IRouteNode>();
            _crate = ObjectFactory.GetInstance<ICrateManager>();
            _telemetryClient = ObjectFactory.GetInstance<TelemetryClient>();
            _security = ObjectFactory.GetInstance<ISecurityServices>();
        }

        public IEnumerable<TViewModel> GetAllActivities<TViewModel>()
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                return uow.PlanRepository.GetActivityQueryUncached().Select(Mapper.Map<TViewModel>);
            }
        }

        public ActivityDO SaveOrUpdateActivity(IUnitOfWork uow, ActivityDO submittedActivityData)
        {
            System.Diagnostics.Stopwatch stopwatch = null;
            DateTime startTime = DateTime.UtcNow;
            bool success = false;

            _telemetryClient.Context.Operation.Name = "Action#SaveOrUpdateAction";

            try
            {
                stopwatch = System.Diagnostics.Stopwatch.StartNew();
                SaveAndUpdateActivity(uow, submittedActivityData, new List<ActivityDO>());

                uow.SaveChanges();
                success = true;
            }
            catch
            {
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _telemetryClient.TrackDependency("Database", "Saving Action with subactions",
                   startTime,
                   stopwatch.Elapsed,
                   success);
            }

            success = false;
            stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var result = uow.PlanRepository.GetById<ActivityDO>(submittedActivityData.Id);
                success = true;
                return result;
            }
            catch
            {
                throw;
            }
            finally
            {
                stopwatch.Stop();
                _telemetryClient.TrackDependency("Database", "Getting Action by id after saving",
                   startTime,
                   stopwatch.Elapsed,
                   success);
            }
        }

        /// <summary>
        /// Update properties and structure of the actions and all descendats.
        /// </summary>
        /// <param name="uow"></param>
        /// <param name="submittedActionData"></param>
        /// <returns></returns>
        //        public async Task<ActionDO> SaveUpdateAndConfigure(IUnitOfWork uow, ActionDO submittedActionData)
        //        {
        //            var pendingConfigurations = new List<ActionDO>();
        //            // Update properties and structure recisurively
        //            var existingAction = SaveAndUpdateRecursive(uow, submittedActionData, pendingConfigurations);
        //
        //            // Change parent if it is necessary
        //            existingAction.ParentRouteNode = submittedActionData.ParentRouteNode;
        //            existingAction.ParentRouteNodeId = submittedActionData.ParentRouteNodeId;
        //
        //            uow.SaveChanges();
        //
        //            foreach (var pendingConfiguration in pendingConfigurations)
        //            {
        //                await ConfigureSingleAction(uow, pendingConfiguration);
        //            }
        //
        //            return uow.ActionRepository.GetByKey(existingAction.Id);
        //        }

        private void UpdateActionProperties(IUnitOfWork uow, ActivityDO submittedActivity)
        {
            var existingAction = uow.PlanRepository.GetById<ActivityDO>(submittedActivity.Id);

            if (existingAction == null)
            {
                throw new Exception("Action was not found");
            }

            UpdateActionProperties(existingAction, submittedActivity);
            uow.SaveChanges();
        }

        private static void UpdateActionProperties(ActivityDO existingActivity, ActivityDO submittedActivity)
        {

            // it is unlikely that we have scenarios when activity template can be changed after activity was created
            //existingActivity.ActivityTemplateId = submittedActivity.ActivityTemplateId;

            existingActivity.Label = submittedActivity.Label;
            existingActivity.CrateStorage = submittedActivity.CrateStorage;
            existingActivity.Ordering = submittedActivity.Ordering;
        }

        private static void RestoreSystemProperties(ActivityDO existingActivity, ActivityDO submittedActivity)
        {
            submittedActivity.AuthorizationTokenId = existingActivity.AuthorizationTokenId;
            }

        private void SaveAndUpdateActivity(IUnitOfWork uow, ActivityDO submittedActiviy, List<ActivityDO> pendingConfiguration)
        {
            RouteTreeHelper.Visit(submittedActiviy, x =>
            {
                var activity = (ActivityDO) x;

                if (activity.Id == Guid.Empty)
                {
                    activity.Id = Guid.NewGuid();
                }
            });

            RouteNodeDO route;
            RouteNodeDO originalAction;
                    if (submittedActiviy.ParentRouteNodeId != null)
                    {
                route = uow.PlanRepository.Reload<RouteNodeDO>(submittedActiviy.ParentRouteNodeId);
                originalAction = route.ChildNodes.FirstOrDefault(x => x.Id == submittedActiviy.Id);
                }
                else
                {
                originalAction = uow.PlanRepository.Reload<RouteNodeDO>(submittedActiviy.Id);
                route = originalAction.ParentRouteNode;
                }


            if (originalAction != null)
                {
                route.ChildNodes.Remove(originalAction);

                var originalActions = RouteTreeHelper.Linearize(originalAction)
                    .ToDictionary(x => x.Id, x => (ActivityDO) x);

                foreach (var submitted in RouteTreeHelper.Linearize(submittedActiviy))
                {
                    ActivityDO existingActivity;

                    if (!originalActions.TryGetValue(submitted.Id, out existingActivity))
                    {
                        pendingConfiguration.Add((ActivityDO) submitted);
                }
            else
            {
                        RestoreSystemProperties(existingActivity, (ActivityDO) submitted);
                }
                }
            }
            else
                    {
                pendingConfiguration.AddRange(RouteTreeHelper.Linearize(submittedActiviy).OfType<ActivityDO>());
                    }

            if (submittedActiviy.Ordering <= 0)
                    {
                route.AddChildWithDefaultOrdering(submittedActiviy);
                    }
            else
                    {
                route.ChildNodes.Add(submittedActiviy);
            }
        }

        public ActivityDO GetById(IUnitOfWork uow, Guid id)
        {
            return uow.PlanRepository.GetById<ActivityDO>(id);
        }

        public async Task<RouteNodeDO> CreateAndConfigure(IUnitOfWork uow, string userId, int actionTemplateId, string label = null, int? order = null, Guid? parentNodeId = null, bool createRoute = false, Guid? authorizationTokenId = null)
        {
            if (parentNodeId != null && createRoute)
            {
                throw new ArgumentException("Parent node id can't be set together with create route flag");
            }

            if (parentNodeId == null && !createRoute)
            {
                throw new ArgumentException("Either Parent node id or create route flag must be set");
            }

            // to avoid null pointer exception while creating parent node if label is null 
            if (label == null)
            {
                label = userId + "_" + actionTemplateId.ToString();
            }

            RouteNodeDO parentNode;
            PlanDO plan = null;

            if (createRoute)
            {
                plan = ObjectFactory.GetInstance<IPlan>().Create(uow, label);

                plan.ChildNodes.Add(parentNode = new SubrouteDO
                {
                    StartingSubroute = true,
                    Name = label + " #1"
                });
            }
            else
            {
                parentNode = uow.PlanRepository.GetById<RouteNodeDO>(parentNodeId);
                
                if (parentNode is PlanDO)
                {
                    if (((PlanDO) parentNode).StartingSubroute == null)
                    {
                        parentNode.ChildNodes.Add(parentNode = new SubrouteDO
                        {
                            StartingSubroute = true,
                            Name = label + " #1"
                        });
                }
                    else
                    {
                        parentNode = ((PlanDO) parentNode).StartingSubroute;
            }

                }
            }

            var activity = new ActivityDO
            {
                Id = Guid.NewGuid(),
                ActivityTemplateId =  actionTemplateId,
                Label = label,
                CrateStorage = _crate.EmptyStorageAsStr(),
                AuthorizationTokenId = authorizationTokenId
            };

            parentNode.AddChild(activity, order);
            
            uow.SaveChanges();

            await ConfigureSingleAction(uow, userId, activity);

            if (createRoute)
            {
                return plan;
            }

            return activity;
        }

        private async Task<ActivityDO> CallActionConfigure (IUnitOfWork uow, string userId, ActivityDO curActivityDO)
        {
            if (curActivityDO == null)
            {
                throw new ArgumentNullException("curActivityDO");
            }

            var tempActionDTO = Mapper.Map<ActivityDTO>(curActivityDO);

            if (!_authorizationToken.ValidateAuthenticationNeeded(uow, userId, tempActionDTO))
            {
                curActivityDO = Mapper.Map<ActivityDO>(tempActionDTO);

                try
                {
                    tempActionDTO = await CallTerminalActionAsync<ActivityDTO>(uow, "configure", curActivityDO, Guid.Empty);
                }
                catch (ArgumentException e)
                {
                    EventManager.TerminalConfigureFailed("<no terminal url>", JsonConvert.SerializeObject(Mapper.Map<ActivityDTO>(curActivityDO)), e.Message, curActivityDO.Id.ToString());
                    throw;
                }
                catch (RestfulServiceException e)
                {
                    // terminal requested token invalidation
                    if (e.StatusCode == 419)
                    {
                        _authorizationToken.InvalidateToken(uow, userId, tempActionDTO);
                    }
                    else
                    {
                        JsonSerializerSettings settings = new JsonSerializerSettings
                        {
                            PreserveReferencesHandling = PreserveReferencesHandling.Objects
                        };
                        var endpoint = _activityTemplate.GetTerminalUrl(curActivityDO.ActivityTemplateId) ?? "<no terminal url>";
                        EventManager.TerminalConfigureFailed(endpoint, JsonConvert.SerializeObject(Mapper.Map<ActivityDTO>(curActivityDO), settings), e.Message, curActivityDO.Id.ToString());
                        throw;
                    }
                }
                catch (Exception e)
                {
                    JsonSerializerSettings settings = new JsonSerializerSettings
                    {
                        PreserveReferencesHandling = PreserveReferencesHandling.Objects
                    };

                    var endpoint = _activityTemplate.GetTerminalUrl(curActivityDO.ActivityTemplateId) ?? "<no terminal url>";
                    EventManager.TerminalConfigureFailed(endpoint, JsonConvert.SerializeObject(Mapper.Map<ActivityDTO>(curActivityDO), settings), e.Message, curActivityDO.Id.ToString());
                    throw;
                }
            }

            return Mapper.Map<ActivityDO>(tempActionDTO);
        }

        private async Task<ActivityDO> ConfigureSingleAction(IUnitOfWork uow, string userId, ActivityDO curActivityDO)
        {
            curActivityDO = await CallActionConfigure(uow, userId, curActivityDO);

            UpdateActionProperties(uow, curActivityDO);

            return curActivityDO;
        }

        public async Task<ActivityDTO> Configure(IUnitOfWork uow, string userId, ActivityDO curActivityDO, bool saveResult = true)
        {
            curActivityDO = await CallActionConfigure(uow, userId, curActivityDO);

            if (saveResult)
            {
                //save the received action as quickly as possible
                    curActivityDO = SaveOrUpdateActivity(uow, curActivityDO);
                    return Mapper.Map<ActivityDTO>(curActivityDO);
                }

            return Mapper.Map<ActivityDTO>(curActivityDO);
        }

        public ActivityDO MapFromDTO(ActivityDTO curActivityDTO)
        {
            ActivityDO submittedActivity = Mapper.Map<ActivityDO>(curActivityDTO);
            return submittedActivity;
        }

        public void Delete(Guid id)
        {
            //we are using Kludge solution for now
            //https://maginot.atlassian.net/wiki/display/SH/Action+Deletion+and+Reordering

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {

                var curAction = uow.PlanRepository.GetById<ActivityDO>(id);
                if (curAction == null)
                {
                    throw new InvalidOperationException("Unknown RouteNode with id: " + id);
                }

                var downStreamActivities = _routeNode.GetDownstreamActivities(uow, curAction).OfType<ActivityDO>();
                //we should clear values of configuration controls
                var directChildren = curAction.GetDescendants().OfType<ActivityDO>();

                //there is no sense of updating children of action being deleted. 
                foreach (var downStreamActivity in downStreamActivities.Except(directChildren))
                {
                    var currentActivity = downStreamActivity;

                    using (var updater = _crate.UpdateStorage(() => currentActivity.CrateStorage))
                    {
                        bool hasChanges = false;
                        foreach (var configurationControls in updater.CrateStorage.CrateContentsOfType<StandardConfigurationControlsCM>())
                        {
                            foreach (IResettable resettable in configurationControls.Controls)
                            {
                                resettable.Reset();
                                hasChanges = true;
                            }
                        }

                        if (!hasChanges)
                        {
                            updater.DiscardChanges();
                        }
                    }
                }

                curAction.RemoveFromParent();

                uow.SaveChanges();
            }
        }

        /// <summary>
        /// The method checks if the action being deleted is CurrentActivity for its ActionList. 
        /// if it is, sets CurrentActivity to the next Action, or null if it is the last action. 
        /// </summary>
        /// <param name="curActionId">Action Id</param>
        /// <param name="uow">Unit of Work</param>
        /// <returns>Returns the current action (if found) or null if not.</returns>
        //        public ActivityDO UpdateCurrentActivity(int curActionId, IUnitOfWork uow)
        //        {
        //            // Find an ActionList for which the action is set as CurrentActivity
        //            // Also, get the whole list of actions for this Action List 
        //            var curActionList = uow.ActionRepository.GetQuery().Where(al => al.Id == curActionId).Include(al => al.Activities).SingleOrDefault();
        //            if (curActionList == null) return null;
        //
        //            // Get current Action
        //            var curAction = curActionList.Activities.SingleOrDefault(a => a.Id == curActionId);
        //            if (curAction == null) return null; // Well, who knows...
        //
        //            // Get ordered list of next Activities 
        //            var activities = curActionList.Activities.Where(a => a.Ordering > curAction.Ordering).OrderBy(a => a.Ordering);
        //            
        //            curActionList.CurrentActivity = activities.FirstOrDefault();
        //
        //            return curAction;
        //        }

        public async Task PrepareToExecute(ActivityDO curActivity, ActionState curActionState, ContainerDO curContainerDO, IUnitOfWork uow)
        {
            EventManager.ActionStarted(curActivity);

            var payload = await Run(uow, curActivity, curActionState, curContainerDO);

            if (payload != null)
            {
                using (var updater = _crate.UpdateStorage(() => curContainerDO.CrateStorage))
                {
                    updater.CrateStorage = _crate.FromDto(payload.CrateStorage);
                }
            }

            uow.SaveChanges();
        }

        // Maxim Kostyrkin: this should be refactored once the TO-DO snippet below is redesigned
        public async Task<PayloadDTO> Run(IUnitOfWork uow, ActivityDO curActivityDO, ActionState curActionState, ContainerDO curContainerDO)
        {
            var eventManager = ObjectFactory.GetInstance<Hub.Managers.Event>();

            if (curActivityDO == null)
            {
                throw new ArgumentNullException("curActivityDO");
            }

            try
            {
                var actionName = curActionState == ActionState.InitialRun ? "Run" : "ChildrenExecuted";
                var payloadDTO = await CallTerminalActionAsync<PayloadDTO>(uow, actionName, curActivityDO, curContainerDO.Id);

                // this will break the infinite loop created for logFr8InternalEvents...

                var plan = uow.PlanRepository.GetById<PlanDO>(curContainerDO.PlanId);

                if (plan != null && plan.Name != "LogFr8InternalEvents")
                {
                    var actionDTO = Mapper.Map<ActivityDTO>(curActivityDO);
                    await eventManager.Publish("ActionExecuted", curActivityDO.Fr8AccountId, curActivityDO.Id.ToString(), JsonConvert.SerializeObject(actionDTO).ToString(), "Success");
                }

                return payloadDTO;

            }
            catch (ArgumentException e)
            {
                EventManager.TerminalRunFailed("<no terminal url>", JsonConvert.SerializeObject(Mapper.Map<ActivityDTO>(curActivityDO)), e.Message, curActivityDO.Id.ToString());
                throw;
            }
            catch (Exception e)
            {
                JsonSerializerSettings settings = new JsonSerializerSettings
                {
                    PreserveReferencesHandling = PreserveReferencesHandling.Objects
                };

                var endpoint = _activityTemplate.GetTerminalUrl(curActivityDO.ActivityTemplateId) ?? "<no terminal url>";
                EventManager.TerminalRunFailed(endpoint, JsonConvert.SerializeObject(Mapper.Map<ActivityDTO>(curActivityDO), settings), e.Message, curActivityDO.Id.ToString());
                throw;
            }
        }


        //looks for the Configuration Controls Crate and Extracts the ManifestSchema
        public StandardConfigurationControlsCM GetControlsManifest(ActivityDO curActivity)
        {
            var control = _crate.GetStorage(curActivity.CrateStorage).CrateContentsOfType<StandardConfigurationControlsCM>().FirstOrDefault();
            //            var curCrateStorage = JsonConvert.DeserializeObject<CrateStorageDTO>(curAction.CrateStorage);
            //            var curControlsCrate =
            //                _crate.GetCratesByManifestType(CrateManifests.STANDARD_CONF_CONTROLS_NANIFEST_NAME, curCrateStorage)
            //                    .FirstOrDefault();

            if (control == null)
            {
                throw new ApplicationException(string.Format("No crate found with Label == \"Configuration_Controls\" and ManifestType == \"{0}\"", CrateManifestTypes.StandardConfigurationControls));
            }


            return control;

        }

        public async Task<ActivityDTO> Activate(ActivityDO curActivityDO)
        {
            try
            {
                //if this action contains nested actions, do not pass them to avoid 
                // circular reference error during JSON serialization (FR-1769)
                //curActivityDO = Mapper.Map<ActivityDO>(curActivityDO); // this doesn't clone activity

                curActivityDO = (ActivityDO)curActivityDO.Clone();

                using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
                {
                    var result = await CallTerminalActionAsync<ActivityDTO>(uow, "activate", curActivityDO, Guid.Empty);

                EventManager.ActionActivated(curActivityDO);
                return result;
            }
            }
            catch (ArgumentException)
            {
                EventManager.TerminalActionActivationFailed("<no terminal url>", JsonConvert.SerializeObject(Mapper.Map<ActivityDTO>(curActivityDO)), curActivityDO.Id.ToString());
                throw;
            }
            catch
            {
                EventManager.TerminalActionActivationFailed(_activityTemplate.GetTerminalUrl(curActivityDO.ActivityTemplateId) ?? "<no terminal url>", JsonConvert.SerializeObject(Mapper.Map<ActivityDTO>(curActivityDO)), curActivityDO.Id.ToString());
                throw;
            }
        }

        public async Task<ActivityDTO> Deactivate(ActivityDO curActivityDO)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                return await CallTerminalActionAsync<ActivityDTO>(uow, "deactivate", curActivityDO, Guid.Empty);
            }
        }
        //private Task<PayloadDTO> RunActionAsync(string actionName, ActionDO curActivityDO, Guid containerId)
        //{
        //    if (actionName == null) throw new ArgumentNullException("actionName");
        //    if (curActivityDO == null) throw new ArgumentNullException("curActivityDO");

        //    var dto = Mapper.Map<ActionDO, ActionDTO>(curActivityDO);
        //    dto.ContainerId = containerId;
        //    _authorizationToken.PrepareAuthToken(dto);

        //    EventManager.ActionDispatched(curActivityDO, containerId);

        //    if (containerId != Guid.Empty)
        //    {
        //        using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
        //        {
        //            var containerDO = uow.ContainerRepository.GetByKey(containerId);
        //            EventManager.ContainerSent(containerDO, curActivityDO);
        //            var reponse = ObjectFactory.GetInstance<ITerminalTransmitter>().CallActionAsync<PayloadDTO>(actionName, dto);
        //            EventManager.ContainerReceived(containerDO, curActivityDO);
        //            return reponse;
        //        }
        //    }

        //    return ObjectFactory.GetInstance<ITerminalTransmitter>().CallActionAsync<PayloadDTO>(actionName, dto);
        //}

        private Task<TResult> CallTerminalActionAsync<TResult>(IUnitOfWork uow, string activityName, ActivityDO curActivityDO, Guid containerId, string curDocumentationSupport = null)
        {
            if (activityName == null) throw new ArgumentNullException("activityName");
            if (curActivityDO == null) throw new ArgumentNullException("curActivityDO");

            var dto = Mapper.Map<ActivityDO, ActivityDTO>(curActivityDO);

            var fr8DataDTO = new Fr8DataDTO
            {
                ContainerId = containerId,
                ActivityDTO = dto
            };

            if (curDocumentationSupport != null)
            {
                dto.DocumentationSupport = curDocumentationSupport;
            }

            _authorizationToken.PrepareAuthToken(uow, dto);

            EventManager.ActionDispatched(curActivityDO, containerId);

            if (containerId != Guid.Empty)
            {
                    var containerDO = uow.ContainerRepository.GetByKey(containerId);
                    EventManager.ContainerSent(containerDO, curActivityDO);
                var reponse = ObjectFactory.GetInstance<ITerminalTransmitter>()
                    .CallActionAsync<TResult>(activityName, fr8DataDTO, containerId.ToString());
                    EventManager.ContainerReceived(containerDO, curActivityDO);
                    return reponse;
                }
            return ObjectFactory.GetInstance<ITerminalTransmitter>().CallActionAsync<TResult>(activityName, fr8DataDTO, containerId.ToString());
        }
        //This method finds and returns single SolutionPageDTO that holds some documentation of Activities that is obtained from a solution by aame
        public async Task<SolutionPageDTO> GetSolutionDocumentation(ActivityDTO activityDTO)
        {
            //Check if a string with "MainPage" keyword is there to signal to Action to provide SolutionPageDTO
            if (!activityDTO.DocumentationSupport.Split(',').Contains("MainPage"))
                throw new Exception("No MainPage value found in DocumentationSupport field value of the ActionDTO");
            SolutionPageDTO solutionPageDTO;
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {  
                //Get the list of all actions that are solutions from database
                var allActivityTemplates = _routeNode.GetSolutions(uow);
                //find the solution by the provided name
                var curActivityTerminalDTO = allActivityTemplates.Single(a => a.Name == activityDTO.ActivityTemplate.Name);
                //prepare an Activity object to be sent to Activity in a Terminal
                //IMPORTANT: this object will not be hold in the database
                //It is used to transfer data
                //as ActivityDTO is the first mean of communication between The Hub and Terminals
                var curSolutionActivityDTO = new ActivityDTO
                {
                    Id = Guid.NewGuid(),
                    Label = curActivityTerminalDTO.Label,
                    AuthToken = new AuthorizationTokenDTO
                    {
                        UserId = null
                    }
                };
                solutionPageDTO = await GetDocumentation(curSolutionActivityDTO);
            }
            return solutionPageDTO;
        }
        private Task<SolutionPageDTO> GetDocumentation(ActivityDTO curActivityDTO)
        {
            //Put a method name so that HandleFr8Request could find correct method in the terminal Action
            var actionName = "documentation";
            curActivityDTO.DocumentationSupport = "MainPage";
            var curContainerId = Guid.Empty;
            //Add log to the database
            EventManager.ActionDispatched(Mapper.Map<ActivityDO>(curActivityDTO), curContainerId);
            var fr8Data = new Fr8DataDTO
            {
                ActivityDTO = curActivityDTO
            };
            //Call the terminal
            return ObjectFactory.GetInstance<ITerminalTransmitter>().CallActionAsync<SolutionPageDTO>(actionName, fr8Data, curContainerId.ToString());
        }
        public List<string> GetSolutionList(string terminalName)
        {
            var solutionNameList = new List<string>();
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var curActivities = uow.ActivityTemplateRepository.GetAll()
                    .Where(a => a.Terminal.Name == terminalName
                        && a.Category == ActivityCategory.Solution)
                        .ToList();
                solutionNameList.AddRange(curActivities.Select(activity => activity.Name));
            }
            return solutionNameList;
        }
        //        public Task<IEnumerable<T>> FindCratesByManifestType<T>(ActionDO curActivityDO, GetCrateDirection direction = GetCrateDirection.None)
        //        {
        //
        //        }

        //        public async Task<IEnumerable<JObject>> FindKeysByCrateManifestType(ActionDO curActivityDO, Data.Interfaces.Manifests.Manifest curSchema, string key,
        //                                                                string fieldName = "name",
        //                                                                GetCrateDirection direction = GetCrateDirection.None)
        //        {
        //            var controlsCrates = _crate.GetCratesByManifestType(curSchema.ManifestName, curActivityDO.CrateStorageDTO()).ToList();
        //
        //            if (direction != GetCrateDirection.None)
        //        {
        //                var upstreamCrates = await ObjectFactory.GetInstance<IRouteNode>()
        //                    .GetCratesByDirection(curActivityDO.Id, curSchema.ManifestName, direction).ConfigureAwait(false);
        //
        //                controlsCrates.AddRange(upstreamCrates);
        //            }
        //
        //            var keys = _crate.GetElementByKey(controlsCrates, key: key, keyFieldName: fieldName);
        //           return keys;
        //        }
    }
}