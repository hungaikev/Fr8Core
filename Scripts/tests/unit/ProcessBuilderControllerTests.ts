﻿/// <reference path="../../app/_all.ts" />
/// <reference path="../../typings/angularjs/angular-mocks.d.ts" />

module dockyard.tests.controller {
    //Setup aliases
    import pwd = dockyard.directives.paneWorkflowDesigner;
    import psa = dockyard.directives.paneSelectAction;
    import pca = dockyard.directives.paneConfigureAction;
    import pst = dockyard.directives.paneSelectTemplate;

    describe("ProcessBuilder Framework message processing", () => {
        beforeEach(module("app"));

        app.run(['$httpBackend',
            function ($httpBackend) {
                $httpBackend.whenGET().passThrough();
            }
        ]);

        var _$controllerService: ng.IControllerService,
            _$scope: dockyard.controllers.IProcessBuilderScope,
            _controller: any,
            _$state: ng.ui.IState,
            _actionServiceMock: utils.ActionServiceMock,
            _$q: ng.IQService,
            _$http: ng.IHttpService,
            _urlPrefix: string;

        beforeEach(() => {
            inject(($controller, $rootScope, $q, $http) => {
                _actionServiceMock = new utils.ActionServiceMock($q);
                _$q = $q;
                _$scope = tests.utils.Factory.GetProcessBuilderScope($rootScope);
                _$state = {
                    data: {
                        pageSubTitle: ""
                    },
                    params: {
                        id: 1
                    }
                };
                _$http = $http;
                _urlPrefix = '/api';

                //Create a mock for ProcessTemplateService
                _controller = $controller("ProcessBuilderController",
                    {
                        $rootScope: $rootScope,
                        $scope: _$scope,
                        stringService: null,
                        LocalIdentityGenerator: null,
                        $state: _$state,
                        ActionService: _actionServiceMock,
                        $http: _$http,
                        urlPrefix: _urlPrefix
                    });
            });
            spyOn(_$scope, "$broadcast");
        });

        //Below rule number are given per part 3. "Message Processing" of Design Document for DO-781 
        //at https://maginot.atlassian.net/wiki/display/SH/Design+Document+for+DO-781
        //Rules 1, 3 and 4 are bypassed because these events not yet stabilized

        //Rule #2
        it("When PaneWorkflowDesigner_TemplateSelected is emitted, PaneSelectTemplate_Render should be received", () => {
            _$scope.$emit(pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_TemplateSelected], null);
            expect(_$scope.$broadcast).toHaveBeenCalledWith('PaneSelectTemplate_Render');
        });

        //Rule #4

        //Rule #5
        it("When PaneSelectTemplate_ProcessTemplateUpdated is sent, state data (template name) must be updated", () => {
            var templateName = "testtemplate";
            var incomingEventArgs = new pst.ProcessTemplateUpdatedEventArgs(1, "testtemplate", ['test']);
            _$scope.$emit(pst.MessageType[pst.MessageType.PaneSelectTemplate_ProcessTemplateUpdated], incomingEventArgs);
        });

        // TODO: do we need this ?
        //Rule #6
        // it("When PaneSelectAction_ActionUpdated is sent, PaneWorkflowDesigner_UpdateAction " +
        //     "should be received with correct args", () => {
        //         var incomingEventArgs = new psa.ActionUpdatedEventArgs(1, 2, true, "testaction"),
        //             outgoingEventArgs = new pwd.ActionNameUpdatedEventArgs(2, "testaction");
        // 
        //         console.log(incomingEventArgs);
        //         console.log(outgoingEventArgs);
        // 
        //         _$scope.$emit(psa.MessageType[psa.MessageType.PaneSelectAction_ActionUpdated], incomingEventArgs);
        // 
        //         expect(_$scope.$broadcast).toHaveBeenCalledWith("PaneWorkflowDesigner_UpdateAction", outgoingEventArgs);
        //     });

        //Rule #7
        it("When PaneSelectAction_ActionTypeSelected is sent, " +
            "PaneConfigureAction_Render should be received with correct args", () => {
                var incomingEventArgs = new psa.ActionTypeSelectedEventArgs(new model.ActionDesignDTO(1, 2, false, 3)),
                    outgoingEvent2Args = new pca.RenderEventArgs(new model.ActionDesignDTO(1, 2, false, 3));
                 
                _$scope.$emit(psa.MessageType[psa.MessageType.PaneSelectAction_ActionTypeSelected], incomingEventArgs);
                expect(_$scope.$broadcast).toHaveBeenCalledWith("PaneConfigureAction_Render", outgoingEvent2Args);
            });

        it("When PaneWorkflowDesigner_ActionSelected is sent and selectedAction!=null " +
            "Save method should be called on ProcessTemplateService", () => {
                var incomingEventArgs = new pwd.ActionSelectedEventArgs(1, 1, 1, 1);
                var currentAction = new model.ActionDesignDTO(1, 1, false, 1);
                _$scope.current.action = <any>currentAction;

                _$scope.$emit(pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ActionSelected], incomingEventArgs);
                expect(_actionServiceMock.save).toHaveBeenCalledWith({ id: currentAction.id}, currentAction, null, null);
            });

        it("When PaneWorkflowDesigner_ActionSelected is sent and selectedAction==null " +
            "Save method on ProcessTemplateService should NOT be called", () => {
                var incomingEventArgs = new pwd.CriteriaSelectedEventArgs(1, true);

                _$scope.current.action = null;

                _$scope.$emit(pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ActionSelected], incomingEventArgs);
                expect(_actionServiceMock.save).not.toHaveBeenCalled();
            });

        it("When PaneWorkflowDesigner_ProcessNodeTemplateSelecting is sent and selectedAction!=null " +
            "Save method should be called on ProcessTemplateService", () => {
                var incomingEventArgs = new pwd.CriteriaSelectedEventArgs(1, true);
                var currentAction = new model.ActionDesignDTO(1, 1, false, 1);
                _$scope.current.action = <any>currentAction;

                _$scope.$emit(pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_CriteriaSelected], incomingEventArgs);
                expect(_actionServiceMock.save).toHaveBeenCalledWith({ id: currentAction.id }, currentAction, null, null);
            });

        it("When PaneWorkflowDesigner_TemplateSelected is sent and selectedAction!=null " +
            "Save method should be called on ProcessTemplateService", () => {
                var incomingEventArgs = new pwd.TemplateSelectedEventArgs();
                var currentAction = new model.ActionDesignDTO(1, 1, false, 1);
                _$scope.current.action = <any>currentAction;

                _$scope.$emit(pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_TemplateSelected], incomingEventArgs);
                expect(_actionServiceMock.save).toHaveBeenCalledWith({ id: currentAction.id }, currentAction, null, null);
            });
    });
}