﻿using Fr8Data.DataTransferObjects;

namespace UtilitiesTesting.Fixtures
{
    partial class FixtureData
    {
        public static PlanEmptyDTO CreateTestPlanDTO()
        {
            return new PlanEmptyDTO()
            {
                Name = "plan1",
                Description = "Description for test plan",
                PlanState = 1,
                Visibility = Fr8Data.States.PlanVisibility.Standard
                //DockyardAccount = FixtureData.TestDockyardAccount1()
            };
        }

        public static string PlanTemplate()
        {
            return @"{    ""id"": 8,    ""name"": ""testplan"",    ""startingPlanNodeDescriptionId"": 20,    ""planNodeDescriptions"": [        {            ""id"": 20,            ""name"": ""Add Payload Manually"",            ""parentNodeId"": 0,            ""transitions"": [                {                    ""id"": 19,                    ""transition"": ""downstream"",                    ""activityDescriptionId"": 23,                    ""planTemplateId"": null,                    ""planId"": null                }            ],            ""activityDescription"": {                ""id"": 25,                ""name"": ""Add Payload Manually"",                ""version"": ""1"",                ""originalId"": ""55d586f5-773b-4b73-94a4-d3f6d4b832fe"",                ""crateStorage"": ""{\r\n  \""crates\"": [\r\n    {\r\n      \""manifestType\"": \""Standard UI Controls\"",\r\n      \""manifestId\"": 6,\r\n      \""manufacturer\"": null,\r\n      \""manifestRegistrar\"": \""www.fr8.co/registry\"",\r\n      \""id\"": \""5c6c1a4d-f68e-46ff-bf08-ffd9c25582ce\"",\r\n      \""label\"": \""Configuration_Controls\"",\r\n      \""contents\"": {\r\n        \""Controls\"": [\r\n          {\r\n            \""name\"": \""Selected_Fields\"",\r\n            \""required\"": true,\r\n            \""value\"": \""[{\\\""Key\\\"":\\\""foo\\\"",\\\""Value\\\"":\\\""bar\\\""}]\"",\r\n            \""label\"": \""Fill the values for other actions\"",\r\n            \""type\"": \""FieldList\"",\r\n            \""selected\"": false,\r\n            \""events\"": [\r\n              {\r\n                \""name\"": \""onChange\"",\r\n                \""handler\"": \""requestConfig\""\r\n              }\r\n            ],\r\n            \""source\"": null,\r\n            \""showDocumentation\"": null,\r\n            \""errorMessage\"": null,\r\n            \""isHidden\"": false,\r\n            \""isCollapsed\"": false\r\n          }\r\n        ]\r\n      },\r\n      \""parentCrateId\"": null,\r\n      \""createTime\"": \""\"",\r\n      \""availability\"": 1\r\n    },\r\n    {\r\n      \""manifestType\"": \""Crate Description\"",\r\n      \""manifestId\"": 32,\r\n      \""manufacturer\"": null,\r\n      \""manifestRegistrar\"": \""www.fr8.co/registry\"",\r\n      \""id\"": \""50b33e38-2787-4011-bf42-3e4de7132a45\"",\r\n      \""label\"": \""Available Run Time Crates\"",\r\n      \""contents\"": {\r\n        \""CrateDescriptions\"": [\r\n          {\r\n            \""manifestId\"": 5,\r\n            \""manifestType\"": \""Standard Payload Data\"",\r\n            \""label\"": \""ManuallyAddedPayload\"",\r\n            \""producedBy\"": \""AddPayloadManually_v1\"",\r\n            \""selected\"": false,\r\n            \""availability\"": 2,\r\n            \""fields\"": []\r\n          }\r\n        ]\r\n      },\r\n      \""parentCrateId\"": null,\r\n      \""createTime\"": \""\"",\r\n      \""availability\"": 2\r\n    },\r\n    {\r\n      \""manifestType\"": \""Field Description\"",\r\n      \""manifestId\"": 3,\r\n      \""manufacturer\"": null,\r\n      \""manifestRegistrar\"": \""www.fr8.co/registry\"",\r\n      \""id\"": \""a7b4d832-dcb4-4b0f-8812-6d8f791d6237\"",\r\n      \""label\"": \""ManuallyAddedPayload\"",\r\n      \""contents\"": {\r\n        \""Fields\"": [\r\n          {\r\n            \""key\"": \""foo\"",\r\n            \""value\"": \""foo\"",\r\n            \""label\"": null,\r\n            \""fieldType\"": null,\r\n            \""isRequired\"": false,\r\n            \""availability\"": 2\r\n          }\r\n        ]\r\n      },\r\n      \""parentCrateId\"": null,\r\n      \""createTime\"": \""\"",\r\n      \""availability\"": 2\r\n    }\r\n  ]\r\n}"",                ""activityTemplateId"": ""43652560-e432-479b-ac6e-6e952e6eaf6a""            },            ""subPlanName"": ""Starting Subplan"",            ""subPlanOriginalId"": ""94135bce-3c9e-4190-b865-698e1dd1f726"",            ""isStartingSubplan"": true        },        {            ""id"": 21,            ""name"": ""Test and Branch"",            ""parentNodeId"": 20,            ""transitions"": [                {                    ""id"": 20,                    ""transition"": ""jump"",                    ""activityDescriptionId"": 24,                    ""planTemplateId"": null,                    ""planId"": null                }            ],            ""activityDescription"": {                ""id"": 23,                ""name"": ""Test and Branch"",                ""version"": ""1"",                ""originalId"": ""98e7ee76-4a65-4438-85a1-7f9053144023"",                ""crateStorage"": ""{\r\n  \""crates\"": [\r\n    {\r\n      \""manifestType\"": \""Field Description\"",\r\n      \""manifestId\"": 3,\r\n      \""manufacturer\"": null,\r\n      \""manifestRegistrar\"": \""www.fr8.co/registry\"",\r\n      \""id\"": \""cf56b358-0fb4-4df7-bc13-c93b3025d94a\"",\r\n      \""label\"": \""Queryable Criteria\"",\r\n      \""contents\"": {\r\n        \""Fields\"": [\r\n          {\r\n            \""key\"": \""foo\"",\r\n            \""value\"": \""foo\"",\r\n            \""label\"": null,\r\n            \""fieldType\"": null,\r\n            \""isRequired\"": false,\r\n            \""availability\"": 2\r\n          }\r\n        ]\r\n      },\r\n      \""parentCrateId\"": null,\r\n      \""createTime\"": \""\"",\r\n      \""availability\"": null\r\n    },\r\n    {\r\n      \""manifestType\"": \""Standard UI Controls\"",\r\n      \""manifestId\"": 6,\r\n      \""manufacturer\"": null,\r\n      \""manifestRegistrar\"": \""www.fr8.co/registry\"",\r\n      \""id\"": \""66b69fc9-d377-4cea-8429-7d1f836af1b4\"",\r\n      \""label\"": \""Configuration_Controls\"",\r\n      \""contents\"": {\r\n        \""Controls\"": [\r\n          {\r\n            \""transitions\"": [\r\n              {\r\n                \""conditions\"": [\r\n                  {\r\n                    \""field\"": \""foo\"",\r\n                    \""operator\"": \""eq\"",\r\n                    \""value\"": \""bar\""\r\n                  }\r\n                ],\r\n                \""transition\"": 2,\r\n                \""targetNodeId\"": \""e9d5438d-9288-4458-af70-4ef17d24461a\"",\r\n                \""_dummyOperationDD\"": {\r\n                  \""type\"": \""DropDownList\"",\r\n                  \""listItems\"": [\r\n                    {\r\n                      \""key\"": \""Jump To Activity\"",\r\n                      \""value\"": \""0\""\r\n                    },\r\n                    {\r\n                      \""key\"": \""Launch Additional Plan\"",\r\n                      \""value\"": \""1\""\r\n                    },\r\n                    {\r\n                      \""key\"": \""Jump To Subplan\"",\r\n                      \""value\"": \""2\""\r\n                    },\r\n                    {\r\n                      \""key\"": \""Stop Processing\"",\r\n                      \""value\"": \""3\""\r\n                    }\r\n                  ],\r\n                  \""value\"": \""2\"",\r\n                  \""selectedKey\"": \""Jump To Subplan\""\r\n                },\r\n                \""_dummySecondaryOperationDD\"": {\r\n                  \""type\"": \""DropDownList\"",\r\n                  \""label\"": \""Select Target Subplan\"",\r\n                  \""listItems\"": [\r\n                    {\r\n                      \""key\"": \""Starting Subplan\"",\r\n                      \""value\"": \""94135bce-3c9e-4190-b865-698e1dd1f726\""\r\n                    },\r\n                    {\r\n                      \""key\"": \""SubPlan-1\"",\r\n                      \""value\"": \""e9d5438d-9288-4458-af70-4ef17d24461a\""\r\n                    }\r\n                  ],\r\n                  \""value\"": \""e9d5438d-9288-4458-af70-4ef17d24461a\"",\r\n                  \""selectedKey\"": \""SubPlan-1\""\r\n                }\r\n              }\r\n            ],\r\n            \""name\"": \""transition\"",\r\n            \""required\"": false,\r\n            \""value\"": null,\r\n            \""label\"": \""Please enter transition\"",\r\n            \""type\"": \""ContainerTransition\"",\r\n            \""selected\"": false,\r\n            \""events\"": [],\r\n            \""source\"": null,\r\n            \""showDocumentation\"": null,\r\n            \""errorMessage\"": null,\r\n            \""isHidden\"": false,\r\n            \""isCollapsed\"": false\r\n          }\r\n        ]\r\n      },\r\n      \""parentCrateId\"": null,\r\n      \""createTime\"": \""\"",\r\n      \""availability\"": 1\r\n    }\r\n  ]\r\n}"",                ""activityTemplateId"": ""7bcf9d8b-68e0-442f-91d9-1ed5e6a72fca""            },            ""subPlanName"": ""Starting Subplan"",            ""subPlanOriginalId"": ""94135bce-3c9e-4190-b865-698e1dd1f726"",            ""isStartingSubplan"": false        },        {            ""id"": 22,            ""name"": ""Add Payload Manually"",            ""parentNodeId"": 0,            ""transitions"": [],            ""activityDescription"": {                ""id"": 24,                ""name"": ""Add Payload Manually"",                ""version"": ""1"",                ""originalId"": ""835a6897-1f39-408c-9a4a-0c49a26dfba3"",                ""crateStorage"": ""{\r\n  \""crates\"": [\r\n    {\r\n      \""manifestType\"": \""Standard UI Controls\"",\r\n      \""manifestId\"": 6,\r\n      \""manufacturer\"": null,\r\n      \""manifestRegistrar\"": \""www.fr8.co/registry\"",\r\n      \""id\"": \""08d88f9c-ad0a-4c00-bc44-60e1f3637102\"",\r\n      \""label\"": \""Configuration_Controls\"",\r\n      \""contents\"": {\r\n        \""Controls\"": [\r\n          {\r\n            \""name\"": \""Selected_Fields\"",\r\n            \""required\"": true,\r\n            \""value\"": \""[{\\\""Key\\\"":\\\""test1\\\"",\\\""Value\\\"":\\\""test2\\\""}]\"",\r\n            \""label\"": \""Fill the values for other actions\"",\r\n            \""type\"": \""FieldList\"",\r\n            \""selected\"": false,\r\n            \""events\"": [\r\n              {\r\n                \""name\"": \""onChange\"",\r\n                \""handler\"": \""requestConfig\""\r\n              }\r\n            ],\r\n            \""source\"": null,\r\n            \""showDocumentation\"": null,\r\n            \""errorMessage\"": null,\r\n            \""isHidden\"": false,\r\n            \""isCollapsed\"": false\r\n          }\r\n        ]\r\n      },\r\n      \""parentCrateId\"": null,\r\n      \""createTime\"": \""\"",\r\n      \""availability\"": 1\r\n    },\r\n    {\r\n      \""manifestType\"": \""Crate Description\"",\r\n      \""manifestId\"": 32,\r\n      \""manufacturer\"": null,\r\n      \""manifestRegistrar\"": \""www.fr8.co/registry\"",\r\n      \""id\"": \""d5d91fe2-fd3f-4ba9-aa3c-4dce8688bdc4\"",\r\n      \""label\"": \""Available Run Time Crates\"",\r\n      \""contents\"": {\r\n        \""CrateDescriptions\"": [\r\n          {\r\n            \""manifestId\"": 5,\r\n            \""manifestType\"": \""Standard Payload Data\"",\r\n            \""label\"": \""ManuallyAddedPayload\"",\r\n            \""producedBy\"": \""AddPayloadManually_v1\"",\r\n            \""selected\"": false,\r\n            \""availability\"": 2,\r\n            \""fields\"": []\r\n          }\r\n        ]\r\n      },\r\n      \""parentCrateId\"": null,\r\n      \""createTime\"": \""\"",\r\n      \""availability\"": 2\r\n    },\r\n    {\r\n      \""manifestType\"": \""Field Description\"",\r\n      \""manifestId\"": 3,\r\n      \""manufacturer\"": null,\r\n      \""manifestRegistrar\"": \""www.fr8.co/registry\"",\r\n      \""id\"": \""1b9f0219-ddb1-4a1e-873a-339992b0384b\"",\r\n      \""label\"": \""ManuallyAddedPayload\"",\r\n      \""contents\"": {\r\n        \""Fields\"": [\r\n          {\r\n            \""key\"": \""test1\"",\r\n            \""value\"": \""test1\"",\r\n            \""label\"": null,\r\n            \""fieldType\"": null,\r\n            \""isRequired\"": false,\r\n            \""availability\"": 2\r\n          }\r\n        ]\r\n      },\r\n      \""parentCrateId\"": null,\r\n      \""createTime\"": \""\"",\r\n      \""availability\"": 2\r\n    }\r\n  ]\r\n}"",                ""activityTemplateId"": ""43652560-e432-479b-ac6e-6e952e6eaf6a""            },            ""subPlanName"": ""SubPlan-1"",            ""subPlanOriginalId"": ""e9d5438d-9288-4458-af70-4ef17d24461a"",            ""isStartingSubplan"": false        }    ],    ""description"": null}";
        }
    }
}