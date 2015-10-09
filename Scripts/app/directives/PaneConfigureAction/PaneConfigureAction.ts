﻿/// <reference path="../../_all.ts" />
module dockyard.directives.paneConfigureAction {
    'use strict';

    export enum MessageType {
        PaneConfigureAction_ActionUpdated,
        PaneConfigureAction_ActionRemoved,
        PaneConfigureAction_InternalAuthentication,
        PaneConfigureAction_ExternalAuthentication
    }

    export class ActionUpdatedEventArgs extends ActionUpdatedEventArgsBase { }

    export class InternalAuthenticationArgs {
        public activityTemplateId: number;

        constructor(activityTemplateId: number) {
            this.activityTemplateId = activityTemplateId;
        }
    }

    export class ExternalAuthenticationArgs {
        public activityTemplateId: number;

        constructor(activityTemplateId: number) {
            this.activityTemplateId = activityTemplateId;
        }
    }

    export class RenderEventArgs {
        public action: interfaces.IActionDTO

        constructor(action: interfaces.IActionDTO) {
            // Clone Action to prevent any issues due to possible mutation of source object
            this.action = angular.extend({}, action);
        }
    }

    export class MapFieldsClickedEventArgs {
        action: model.ActionDTO;

        constructor(action: model.ActionDTO) {
            this.action = action;
        }
    }

    export class ActionRemovedEventArgs {
        public id: number;
        public isTempId: boolean;

        constructor(id: number, isTempId: boolean) {
            this.id = id;
            this.isTempId = isTempId;
        }
    }

    export interface IPaneConfigureActionScope extends ng.IScope {
        onActionChanged: (newValue: model.ActionDTO, oldValue: model.ActionDTO, scope: IPaneConfigureActionScope) => void;
        action: interfaces.IActionDTO;
        isVisible: boolean;
        removeAction: () => void;
        currentAction: interfaces.IActionVM;
        configurationControls: ng.resource.IResource<model.ControlsList> | model.ControlsList;
        crateStorage: ng.resource.IResource<model.CrateStorage> | model.CrateStorage;
        mapFields: (scope: IPaneConfigureActionScope) => void;
        processing: boolean;
    }

    export class CancelledEventArgs extends CancelledEventArgsBase { }

    //More detail on creating directives in TypeScript: 
    //http://blog.aaronholmes.net/writing-angularjs-directives-as-typescript-classes/
    class PaneConfigureAction implements ng.IDirective {
        public link: (scope: IPaneConfigureActionScope, element: ng.IAugmentedJQuery, attrs: ng.IAttributes) => void;
        public templateUrl = '/AngularTemplate/PaneConfigureAction';
        public controller: ($scope: IPaneConfigureActionScope, element: ng.IAugmentedJQuery, attrs: ng.IAttributes) => void;
        public scope = {
            currentAction: '='
        };
        public restrict = 'E';
        private _$element: ng.IAugmentedJQuery;
        private _$scope: IPaneConfigureActionScope;
        private configurationWatchUnregisterer: Function;

        constructor(
            private $rootScope: interfaces.IAppRootScope,
            private ActionService: services.IActionService,
            private crateHelper: services.CrateHelper,
            private $filter: ng.IFilterService,
            private $timeout: ng.ITimeoutService
        ) {

            PaneConfigureAction.prototype.link = (
                scope: IPaneConfigureActionScope,
                element: ng.IAugmentedJQuery,
                attrs: ng.IAttributes) => {

                //Link function goes here
            };

            PaneConfigureAction.prototype.controller = (
                $scope: IPaneConfigureActionScope,
                $element: ng.IAugmentedJQuery,
                $attrs: ng.IAttributes) => {
                this._$element = $element;
                this._$scope = $scope;

                this.onRender();
                $scope.$on("onChange", <any>angular.bind(this, this.onControlChange));
                $scope.removeAction = <any>angular.bind(this, this.removeAction);
            };
        }

        private onConfigurationChanged(newValue: model.ControlsList, oldValue: model.ControlsList, scope: IPaneConfigureActionScope) {

            if (!newValue || !newValue.fields || newValue.fields === oldValue.fields || newValue.fields.length == 0) return;
            this.crateHelper.mergeControlListCrate(
                scope.currentAction.configurationControls,
                scope.currentAction.crateStorage
            );
            scope.currentAction.crateStorage.crateDTO = scope.currentAction.crateStorage.crates //backend expects crates on CrateDTO field
            this.ActionService.save({ id: scope.currentAction.id },
                scope.currentAction, null, null);
        }

        private removeAction() {
            if (!this._$scope.currentAction.isTempId) {
                this.ActionService.delete({
                    id: this._$scope.currentAction.id
                });
            }

            this._$scope.$emit(
                MessageType[MessageType.PaneConfigureAction_ActionRemoved],
                new ActionRemovedEventArgs(this._$scope.currentAction.id, this._$scope.currentAction.isTempId)
            );

            this._$scope.currentAction = null;
            this._$scope.isVisible = false;
        };

        private onControlChange(event: ng.IAngularEvent, eventArgs: ChangeEventArgs) {
            var scope = <IPaneConfigureActionScope>event.currentScope;
            // Check if this event is defined for the current field
            var fieldName = eventArgs.fieldName;
            var fieldList = scope.currentAction.configurationControls.fields;


            // Find the configuration field object for which the event has fired
            fieldList = <Array<model.ControlDefinitionDTO>>this.$filter('filter')(fieldList, { name: fieldName }, true);
            if (fieldList.length == 0 || !fieldList[0].events || fieldList[0].events.length == 0) return;
            var field = fieldList[0];

            // Find the onChange event object
            var eventHandlerList = <Array<model.ControlEvent>>this.$filter('filter')(field.events, { name: 'onChange' }, true);
            if (eventHandlerList.length == 0) return;
            var fieldEvent = eventHandlerList[0];

            if (fieldEvent.handler === 'requestConfig') {
                this.crateHelper.mergeControlListCrate(
                    scope.currentAction.configurationControls,
                    scope.currentAction.crateStorage
                );
                scope.currentAction.crateStorage.crateDTO = scope.currentAction.crateStorage.crates //backend expects crates on CrateDTO field
                
                // Block the pane to prevent user from making more changes since pane controls may change
                this.blockUI();

                this.loadConfiguration();
            }
        }

        private blockUI() {
            //Metronic.blockUI({ target:  });
        }

        private onRender() {
            if (this.configurationWatchUnregisterer) this.configurationWatchUnregisterer();

            // Avoid unnecessary Save while 
            if (this.configurationWatchUnregisterer) {
                this.configurationWatchUnregisterer();
            }
            //for now ignore actions which were not saved in the database
            if (this._$scope.currentAction.isTempId) return;

            // Get configuration settings template from the server if the current action does not 
            // contain those or user has selected another action template.
            //if (scope.currentAction.crateStorage == null
            //    || scope.currentAction.configurationControls.fields == null
            //    || scope.currentAction.configurationControls.fields.length == 0
            //    || (eventArgs.action.id == this._currentAction.id &&
            //        eventArgs.action.actionTemplateId != this._currentAction.actionTemplateId)) {
            //FOR NOW we're going to simplify things by always checking with this server for a new configuration

            // Without $timeout the directive's copy of currentAction does not have enough time 
            // to refresh after being assigned newly selected Action on ProcessBuilderController
            // and as a result it contained old action. 
            this.$timeout(() => {
                if (this._$scope.currentAction.activityTemplateId > 0) {
                    this.loadConfiguration();
                }            
            }, 300);
        }

        // Here we look for Crate with ManifestType == 'Standard Configuration Controls'.
        // We parse its contents and put it into currentAction.configurationControls structure.
        private loadConfiguration() {
            // Block pane and show pane-level 'loading' spinner
            this._$scope.processing = true;
            var activityTemplateName = this._$scope.currentAction.activityTemplateName; // preserve activity name

            if (this.configurationWatchUnregisterer) {
                this.configurationWatchUnregisterer();
            }

            this.ActionService.configure(this._$scope.currentAction).$promise.then((res: any) => {
                // Check if authentication is required.
                if (this.crateHelper.hasCrateOfManifestType(res.crateStorage, 'Standard Authentication')) {
                    var authCrate = this.crateHelper
                        .findByManifestType(res.crateStorage, 'Standard Authentication');

                    var authMS = angular.fromJson(authCrate.contents);

                    // Dockyard auth mode.
                    if (authMS.Mode == 1) {
                        this._$scope.$emit(
                            MessageType[MessageType.PaneConfigureAction_InternalAuthentication],
                            new InternalAuthenticationArgs(res.activityTemplateId)
                        );
                    }

                    // External auth mode.
                    else {
                        // self.$window.open(authMS.Url, '', 'width=400, height=500, location=no, status=no');
                        this._$scope.$emit(
                            MessageType[MessageType.PaneConfigureAction_ExternalAuthentication],
                            new ExternalAuthenticationArgs(res.activityTemplateId)
                        );
                    }

                    this._$scope.processing = false;
                    return;
                }

                // Unblock pane
                this._$scope.processing = false;

                // Assign name to res rather than currentAction to prevent 
                // $watches from unnecessarily triggering
                res.activityTemplateName = activityTemplateName;
                this._$scope.currentAction = res;
                (<any>this._$scope.currentAction).configurationControls =
                this.crateHelper.createControlListFromCrateStorage(this._$scope.currentAction.crateStorage);

                this.$timeout(() => { // let the control list create, we don't want false change notification during creation process
                    this.configurationWatchUnregisterer = this._$scope.$watch<model.ControlsList>(
                        (scope: IPaneConfigureActionScope) => this._$scope.currentAction.configurationControls,
                        <any>angular.bind(this, this.onConfigurationChanged),
                        true);
                }, 1000);
            });
        }

        //The factory function returns Directive object as per Angular requirements
        public static Factory() {
            var directive = (
                $rootScope: interfaces.IAppRootScope,
                ActionService,
                crateHelper: services.CrateHelper,
                $filter: ng.IFilterService,
                $timeout: ng.ITimeoutService
            ) => {

                return new PaneConfigureAction($rootScope, ActionService,
                    crateHelper, $filter, $timeout);
            };

            directive['$inject'] = ['$rootScope', 'ActionService',
                'CrateHelper', '$filter', '$timeout'];

            return directive;
        }
    }

    app.directive('paneConfigureAction', PaneConfigureAction.Factory());
}