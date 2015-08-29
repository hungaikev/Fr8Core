﻿/// <reference path="../_all.ts" />

/*
    Detail (view/add/edit) controller
*/
module dockyard.controllers {
    'use strict';

    //Setup aliases
    import pwd = dockyard.directives.paneWorkflowDesigner;
    import pdc = dockyard.directives.paneDefineCriteria;
    import psa = dockyard.directives.paneSelectAction;
    import pca = dockyard.directives.paneConfigureAction;
    import pst = dockyard.directives.paneSelectTemplate;
    import pcm = dockyard.directives.paneConfigureMapping;

    class ProcessBuilderController {
        // $inject annotation.
        // It provides $injector with information about dependencies to be injected into constructor
        // it is better to have it close to the constructor, because the parameters must match in count and type.
        // See http://docs.angularjs.org/guide/di


        public static $inject = [
            '$rootScope',
            '$scope',
            'StringService',
            'LocalIdentityGenerator',
            '$state',
            'ActionService',
            '$q',
            '$http',
            'urlPrefix'
        ];

        private _scope: interfaces.IProcessBuilderScope;

            constructor(
                private $rootScope: interfaces.IAppRootScope,
                private $scope: interfaces.IProcessBuilderScope,
                private StringService: services.IStringService,
                private LocalIdentityGenerator: services.ILocalIdentityGenerator,
                private $state: ng.ui.IState,
                private ActionService: services.IActionService,
                private $q: ng.IQService,
                private $http: ng.IHttpService,
                private urlPrefix: string
            ) {
            this._scope = $scope;
            this._scope.processTemplateId = $state.params.id;

            this._scope.processNodeTemplates = [];
            this._scope.fields = [
                new model.Field('envelope.name', '[Envelope].Name'),
                new model.Field('envelope.date', '[Envelope].Date')
            ];

            this._scope.curNodeId = null;
            this._scope.curNodeIsTempId = false;

            this._scope.Cancel = angular.bind(this, this.Cancel);
            this._scope.Save = angular.bind(this, this.onSave);

            this.setupMessageProcessing();
        }

        /*
            Mapping of incoming messages to handlers
        */
        private setupMessageProcessing() {

            //Process Designer Pane events
            this._scope.$on(pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ProcessNodeTemplateAdding],
                (event: ng.IAngularEvent, eventArgs: pwd.ProcessNodeTemplateAddingEventArgs) => this.PaneWorkflowDesigner_ProcessNodeTemplateAdding(eventArgs));
            this._scope.$on(pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ProcessNodeTemplateSelecting],
                (event: ng.IAngularEvent, eventArgs: pwd.ProcessNodeTemplateSelectingEventArgs) => this.PaneWorkflowDesigner_ProcessNodeTemplateSelecting(eventArgs));
            this._scope.$on(pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ActionAdding],
                (event: ng.IAngularEvent, eventArgs: pwd.ActionAddingEventArgs) => this.PaneWorkflowDesigner_ActionAdding(eventArgs));
            this._scope.$on(pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ActionSelecting],
                (event: ng.IAngularEvent, eventArgs: pwd.ActionSelectingEventArgs) => this.PaneWorkflowDesigner_ActionSelecting(eventArgs));
            this._scope.$on(pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_TemplateSelecting],
                (event: ng.IAngularEvent, eventArgs: pwd.TemplateSelectingEventArgs) => this.PaneWorkflowDesigner_TemplateSelecting(eventArgs));

            //Define Criteria Pane events
            this._scope.$on(pdc.MessageType[pdc.MessageType.PaneDefineCriteria_ProcessNodeTemplateRemoving],
                (event: ng.IAngularEvent, eventArgs: pdc.ProcessNodeTemplateRemovingEventArgs) => this.PaneDefineCriteria_ProcessNodeTemplateRemoving(eventArgs));
            this._scope.$on(pdc.MessageType[pdc.MessageType.PaneDefineCriteria_Cancelling],
                (event: ng.IAngularEvent) => this.PaneDefineCriteria_Cancelling());
            this._scope.$on(pdc.MessageType[pdc.MessageType.PaneDefineCriteria_ProcessNodeTemplateUpdating],
                (event: ng.IAngularEvent, eventArgs: pdc.ProcessNodeTemplateUpdatingEventArgs) => this.PaneDefineCriteria_ProcessNodeTemplateUpdating(eventArgs));

            //Process Configure Action Pane events
            this._scope.$on(pca.MessageType[pca.MessageType.PaneConfigureAction_ActionUpdated],
                (event: ng.IAngularEvent, eventArgs: pca.ActionUpdatedEventArgs) => this.PaneConfigureAction_ActionUpdated(eventArgs));

            //Process Select Action Pane events
            this._scope.$on(psa.MessageType[psa.MessageType.PaneSelectAction_ActionTypeSelected],
                (event: ng.IAngularEvent, eventArgs: psa.ActionTypeSelectedEventArgs) => this.PaneSelectAction_ActionTypeSelected(eventArgs));
            // TODO: do we need this any more?
            // this._scope.$on(psa.MessageType[psa.MessageType.PaneSelectAction_ActionUpdated],
            //     (event: ng.IAngularEvent, eventArgs: psa.ActionUpdatedEventArgs) => this.PaneSelectAction_ActionUpdated(eventArgs));
            this._scope.$on(psa.MessageType[psa.MessageType.PaneSelectAction_ActionRemoved],
                (event: ng.IAngularEvent, eventArgs: psa.ActionRemovedEventArgs) => this.PaneSelectAction_ActionRemoved(eventArgs));

            //Process Select Template Pane events
            this._scope.$on(pst.MessageType[pst.MessageType.PaneSelectTemplate_ProcessTemplateUpdated],
                (event: ng.IAngularEvent, eventArgs: pst.ProcessTemplateUpdatedEventArgs) => {
                    this.$state.data.pageSubTitle = eventArgs.processTemplateName
                });
        }
         
        // Find criteria by Id.
        private findProcessNodeTemplate(id: number): model.ProcessNodeTemplate {
            var i;
            for (i = 0; i < this._scope.processNodeTemplates.length; ++i) {
                if (this._scope.processNodeTemplates[i].id === id) {
                    return this._scope.processNodeTemplates[i];
                }
            }
            return null;
        }

        private saveProcessNodeTemplate(callback: (args: pdc.SaveCallbackArgs) => void) {
            if (this._scope.curNodeId != null) {
                this._scope.$broadcast(
                    pdc.MessageType[pdc.MessageType.PaneDefineCriteria_Save],
                    new pdc.SaveEventArgs(callback)
                    );
                this._scope.curNodeId = null;
                this._scope.curNodeIsTempId = false;
            }
            else {
                callback(null);
            }
        }

        /*
            Handles message 'PaneDefineCriteria_ProcessNodeTemplateUpdating'
        */
        private PaneDefineCriteria_ProcessNodeTemplateUpdating(eventArgs: pdc.ProcessNodeTemplateUpdatingEventArgs) {
            console.log('ProcessBuilderController::PaneDefineCriteria_ProcessNodeTemplateUpdating', eventArgs);

            if (eventArgs.processNodeTemplateTempId) {
                this._scope.$broadcast(
                    pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ProcessNodeTemplateTempIdReplaced],
                    new pwd.ProcessNodeTemplateTempIdReplacedEventArgs(eventArgs.processNodeTemplateTempId, eventArgs.processNodeTemplateId)
                    );
            }

            this._scope.$broadcast(
                pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ProcessNodeTemplateNameUpdated],
                new pwd.ProcessNodeTemplateNameUpdatedEventArgs(eventArgs.processNodeTemplateId, eventArgs.name)
                );
        }

        /*
            Handles message 'PaneDefineCriteria_CriteriaRemoving'
        */
        private PaneDefineCriteria_ProcessNodeTemplateRemoving(eventArgs: pdc.ProcessNodeTemplateRemovingEventArgs) {
            console.log('ProcessBuilderController::PaneDefineCriteria_ProcessNodeTemplateRemoving', eventArgs);

            // Tell Workflow Designer to remove criteria.
            this._scope.$broadcast(
                pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ProcessNodeTemplateRemoved],
                new pwd.ProcessNodeTemplateRemovedEventArgs(eventArgs.processNodeTemplateId, eventArgs.isTempId)
            );

            // Hide Define Criteria pane.
            this._scope.$broadcast(pdc.MessageType[pdc.MessageType.PaneDefineCriteria_Hide]);
        }

        /*
            Handles message 'PaneDefineCriteria_Cancelling'
        */
        private PaneDefineCriteria_Cancelling() {
            console.log('ProcessBuilderController::PaneDefineCriteria_Cancelling');

            // If user worked with temporary (not saved criteria), remove criteria from Workflow Designer.
            if (this._scope.curNodeId
                && this._scope.curNodeIsTempId) {

                this._scope.$broadcast(
                    pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ProcessNodeTemplateRemoved],
                    new pwd.ProcessNodeTemplateRemovedEventArgs(
                        this._scope.curNodeId,
                        this._scope.curNodeIsTempId
                    )
                );
            }

            // Hide DefineCriteria pane.
            this._scope.$broadcast(
                pdc.MessageType[pdc.MessageType.PaneDefineCriteria_Hide]
                );

            // Set currentCriteria to null, marking that no criteria is currently selected.
            this._scope.curNodeId = null;
            this._scope.curNodeIsTempId = false;
        }

        /*
            Handles message 'WorkflowDesignerPane_CriteriaAdding'
        */
        private PaneWorkflowDesigner_ProcessNodeTemplateAdding(eventArgs: pwd.ProcessNodeTemplateAddingEventArgs) {
            console.log('ProcessBuilderController::PaneWorkflowDesigner_CriteriaAdding', eventArgs);

            var self = this;
            this.saveProcessNodeTemplate(function () {
                // Make Workflow Designer add newly created criteria.
                self._scope.$broadcast(
                    pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ProcessNodeTemplateAdded],
                    new pwd.ProcessNodeTemplateAddedEventArgs(self.LocalIdentityGenerator.getNextId(), true, 'New criteria')
                    );
            });
        }

        /*
            Handles message 'WorkflowDesignerPane_CriteriaSelected'
        */
        private PaneWorkflowDesigner_ProcessNodeTemplateSelecting(eventArgs: pwd.ProcessNodeTemplateSelectingEventArgs) {
            console.log("ProcessBuilderController::PaneWorkflowDesigner_CriteriaSelected", eventArgs);
            var self = this;
            this.saveProcessNodeTemplate(function () {
                self.SaveAction(function () {
                    if (self._scope.currentAction != null) {
                        self._scope.$broadcast(
                            pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ActionNameUpdated],
                            new pwd.ActionNameUpdatedEventArgs(self._scope.currentAction.id, self._scope.currentAction.userLabel)
                            );
                    }

                    self._scope.currentAction = null; // the prev action is apparently unselected

                    // Set current Criteria to currently selected criteria.
                    self._scope.curNodeId = eventArgs.id;
                    self._scope.curNodeIsTempId = eventArgs.isTempId;

                    var scope = self._scope;
                    // Hide Select Template Pane
                    scope.$broadcast(pst.MessageType[pst.MessageType.PaneSelectTemplate_Hide]);

                    // Show Define Criteria Pane
                    scope.$broadcast(
                        pdc.MessageType[pdc.MessageType.PaneDefineCriteria_Render],
                        new pdc.RenderEventArgs(scope.fields, self._scope.processTemplateId, eventArgs.id, eventArgs.isTempId)
                        );

                    // Hide Select Action Pane
                    scope.$broadcast(psa.MessageType[psa.MessageType.PaneSelectAction_Hide]);
                
                    // Hide Configure Action Pane
                    scope.$broadcast(pca.MessageType[pca.MessageType.PaneConfigureAction_Hide]);
                });
            });
        }

        /*
            Handles message 'PaneWorkflowDesigner_ActionAdding'
        */
        private PaneWorkflowDesigner_ActionAdding(eventArgs: pwd.ActionAddingEventArgs) {
            console.log('ProcessBuilderController::PaneWorkflowDesigner_ActionAdding', eventArgs);

            var self = this;
            this.saveProcessNodeTemplate(function (args: pdc.SaveCallbackArgs) {
                // Generate next Id.
                var id = self.LocalIdentityGenerator.getNextId();

                var processNodeTemplateId = (args ? args.id : eventArgs.processNodeTemplateId);
                var url = self.urlPrefix
                    + '/actionList/byProcessNodeTemplate/?id=' + processNodeTemplateId.toString()
                    + '&actionListType=' + eventArgs.actionListType.toString();

                self.$http.get(url)
                    .then(function (resp) {
                        var data = <any>resp.data;
                        var actionListId = data.id;

                        // Create action object.
                        var action = new model.Action(
                            processNodeTemplateId,
                            id,
                            true,
                            actionListId
                            );

                        action.userLabel = 'New Action #' + Math.abs(id).toString();

                        self._scope.currentAction = action.toActionVM();

                        // Add action to Workflow Designer.
                        self._scope.$broadcast(
                            pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ActionAdded],
                            new pwd.ActionAddedEventArgs(action.processNodeTemplateId, action.clone(), eventArgs.actionListType)
                            );
                    });
            });
        }

        /*
            Handles message 'WorkflowDesignerPane_ActionSelecting'
        */
        private PaneWorkflowDesigner_ActionSelecting(eventArgs: pwd.ActionSelectingEventArgs) {
            console.log("ProcessBuilderController: action selected", eventArgs);

            var self = this;

            self.saveProcessNodeTemplate(function () {
                var originalId = null;
                if (self._scope.currentAction) {
                    originalId = self._scope.currentAction.id;
                }

                self.SaveAction(function (savedAction: any) {
                    if (self._scope.currentAction != null) {
                        self._scope.$broadcast(
                            pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ActionNameUpdated],
                            new pwd.ActionNameUpdatedEventArgs(self._scope.currentAction.id, self._scope.currentAction.userLabel)
                            );
                    }

                    var wasTemporaryAction = (originalId == eventArgs.actionId);

                    var actionId = wasTemporaryAction && savedAction
                        ? savedAction[0].id
                        : eventArgs.actionId;

                    var getData = { id: actionId };
                    self._scope.currentAction = self.ActionService.get(getData);

                    //Render Select Action Pane
                    var eArgs = new psa.RenderEventArgs(
                        eventArgs.processNodeTemplateId,
                        actionId,
                        false,
                        eventArgs.actionListId);

                    self._scope.$broadcast(pst.MessageType[pst.MessageType.PaneSelectTemplate_Hide]);
                    self._scope.$broadcast(pdc.MessageType[pdc.MessageType.PaneDefineCriteria_Hide]);
                    self._scope.$broadcast(
                        psa.MessageType[psa.MessageType.PaneSelectAction_Render],
                        eArgs
                        );
                });
            });
        }

        /*
            Handles message 'WorkflowDesignerPane_TemplateSelected'
        */
        private PaneWorkflowDesigner_TemplateSelecting(eventArgs: pwd.TemplateSelectingEventArgs) {
            console.log("ProcessBuilderController: template selected");

            var scope = this._scope;
            this.SaveAction(function () {
                if (scope.currentAction != null) {
                    scope.$broadcast(
                        pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ActionNameUpdated],
                        new pwd.ActionNameUpdatedEventArgs(scope.currentAction.id, scope.currentAction.userLabel)
                        );
                }

                scope.currentAction = null; // action is apparently unselected

                //Show Select Template Pane
                var eArgs = new directives.paneSelectTemplate.RenderEventArgs();
                scope.$broadcast(pst.MessageType[pst.MessageType.PaneSelectTemplate_Render]);

                //Hide Define Criteria Pane
                scope.$broadcast(pdc.MessageType[pdc.MessageType.PaneDefineCriteria_Hide]);

                //Hide Select Action Pane
                scope.$broadcast(psa.MessageType[psa.MessageType.PaneSelectAction_Hide]);
                
                //Hide Configure Action Pane
                scope.$broadcast(pca.MessageType[pca.MessageType.PaneConfigureAction_Hide]);
            });
        }

        /*
            Handles message 'ConfigureActionPane_ActionUpdated'
        */
        private PaneConfigureAction_ActionUpdated(eventArgs: pca.ActionUpdatedEventArgs) {
            //Force update on Select Action Pane (FOR DEMO ONLY, NOT IN DESIGN DOCUMENT)
            var psaArgs = new psa.UpdateActionEventArgs(
                eventArgs.criteriaId,
                eventArgs.actionId,
                eventArgs.isTempId);

            this._scope.$broadcast(
                psa.MessageType[psa.MessageType.PaneSelectAction_UpdateAction],
                psaArgs);
        }

        /*
            Handles message 'SelectActionPane_ActionTypeSelected'
        */
        private PaneSelectAction_ActionTypeSelected(eventArgs: psa.ActionTypeSelectedEventArgs) {
            //Render Pane Configure Action 
            var pcaEventArgs = new pca.RenderEventArgs(
                eventArgs.processNodeTemplateId,
                eventArgs.id,
                eventArgs.isTempId,
                eventArgs.actionListId);
                
            this._scope.$broadcast(pca.MessageType[pca.MessageType.PaneConfigureAction_Render], pcaEventArgs);

            //Render Pane Configure Mapping 
            //var pcmEventArgs = new pcm.RenderEventArgs(
            //    eventArgs.processNodeTemplateId,
            //    eventArgs.id,
            //    eventArgs.isTempId);

            //this._scope.$broadcast(pcm.MessageType[pcm.MessageType.PaneConfigureMapping_Render], pcmEventArgs);
        }
         
        // TODO: do we need this?
        // /*
        //     Handles message 'PaneSelectAction_ActionUpdated'
        // */
        // private PaneSelectAction_ActionUpdated(eventArgs: psa.ActionUpdatedEventArgs) {
        //     //Update Pane Workflow Designer
        //     var eArgs = new pwd.ActionNameUpdatedEventArgs(
        //         eventArgs.actionId,
        //         eventArgs.actionName);
        //     this._scope.$broadcast(pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ActionNameUpdated], eArgs);
        // }

        /*
            Handles message 'PaneSelectAction_ActionRemoved'
        */
        private PaneSelectAction_ActionRemoved(eventArgs: psa.ActionRemovedEventArgs) {
            this._scope.$broadcast(
                pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ActionRemoved],
                new pwd.ActionRemovedEventArgs(eventArgs.id, eventArgs.isTempId)
                );
        }

        private onSave() {
            var self = this;

            return this.SaveAction(function () {
                if (self._scope.currentAction != null) {
                    self._scope.$broadcast(
                        pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ActionNameUpdated],
                        new pwd.ActionNameUpdatedEventArgs(self._scope.currentAction.id, self._scope.currentAction.userLabel)
                        );
                }
            });
        }

        public SaveAction(callback: (data: interfaces.IAction) => void) {
            var self = this;

            //If an action is selected, save it
            if (self._scope.currentAction != null) {
                debugger;

                var promise = self.ActionService.save(
                    {
                        id: self._scope.currentAction.id
                    },
                    self._scope.currentAction,
                    null,
                    null
                    ).$promise;

                promise.then(function (data: any) {
                    if (self._scope.currentAction.isTempId) {
                        self._scope.$broadcast(
                            pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ActionTempIdReplaced],
                            new pwd.ActionTempIdReplacedEventArgs(self._scope.currentAction.id, data[0].id)
                            );

                        if (data) {
                            self._scope.currentAction.id = data[0].Id;
                            self._scope.currentAction.isTempId = false;
                        }
                    }

                    if (callback) {
                        callback(data);
                    }
                });

                return promise;
            }
            else {
                if (callback) {
                    callback(null);
                }
            }
        }

        public Cancel() {
            this._scope.currentAction = null;
            this.HideActionPanes();
        }

        private HideActionPanes() {
            //Hide Select Action Pane
            this._scope.$broadcast(psa.MessageType[psa.MessageType.PaneSelectAction_Hide]);

            //Hide Configure Mapping Pane
            this._scope.$broadcast(pcm.MessageType[pcm.MessageType.PaneConfigureMapping_Hide]);

            //Hide Configure Action Pane
            this._scope.$broadcast(pca.MessageType[pca.MessageType.PaneConfigureAction_Hide]);
        }
    }

    app.run([
        "$httpBackend", "urlPrefix", ($httpBackend, urlPrefix) => {
            var actions: interfaces.IAction =
                {
                    actionType: "test action type",
                    configurationSettings: new model.ConfigurationSettings(),
                    processNodeTemplateId: 1,
                    id: 1,
                    isTempId: false,
                    fieldMappingSettings: "",
                    userLabel: "test",
                    tempId: 0,
                    actionListId: 0
                };

            $httpBackend
                .whenGET(urlPrefix + "/Action/1")
                .respond(actions);

            $httpBackend
                .whenPOST(urlPrefix + "/Action/1")
                .respond(function (method, url, data) {
                    return data;
                })
    }
    ]);

    app.controller('ProcessBuilderController', ProcessBuilderController);
} 