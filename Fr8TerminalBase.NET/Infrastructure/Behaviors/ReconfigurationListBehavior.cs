﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fr8.TerminalBase.Models;

namespace Fr8.TerminalBase.Infrastructure.Behaviors
{
    /// <summary>
    /// A single ReconfigurationList item.
    /// </summary>
    public class ConfigurationRequest
    {
        /// <summary>
        /// Delegate that used by ReconfigurationList algorithm to check whether activity exists in the list of solution's child-nodes.
        /// </summary>
        public Func<ReconfigurationContext, Task<bool>> HasActivityMethod { get; set; }

        /// <summary>
        /// Delegate that used by ReconfigurationList algorithm to create a new activity and add it to solution child-nodes.
        /// </summary>
        public Func<ReconfigurationContext, Task<ActivityPayload>> CreateActivityMethod { get; set; }

        /// <summary>
        /// Delegate that used by ReconfigurationList algorithm to configure existing activity in the list of solution child-nodes.
        /// </summary>
        public Func<ReconfigurationContext, Task<ActivityPayload>> ConfigureActivityMethod { get; set; }

        /// <summary>
        /// Ordering number of a child activity to be configured.
        /// </summary>
        public int ChildActivityIndex { get; set; }
    }

    /// <summary>
    /// Context of ReconfigurationList behavior that is passed to ConfigurationRequest's delegates.
    /// </summary>
    public class ReconfigurationContext
    {
        public ReconfigurationContext()
        {
            AdditionalRequests = new List<ConfigurationRequest>();
        }

        /// <summary>
        /// Solution activity that ReconfigurationLists is run for.
        /// </summary>
        public ActivityPayload SolutionActivity { get; set; }

        /// <summary>
        /// Current AuthToken.
        /// </summary>
        public AuthorizationToken AuthToken { get; set; }

        /// <summary>
        /// The list of initial requests.
        /// </summary>
        public IReadOnlyList<ConfigurationRequest> Requests { get; set; }

        /// <summary>
        /// The list of additional requests, that is generated by one of ConfigurationRequest's delegates.
        /// List of additional requests is added to the end of ReconfigurationList's processing queue.
        /// </summary>
        public List<ConfigurationRequest> AdditionalRequests { get; set; }
    }


    /// <summary>
    /// Class that implements ReconfigurationList algorithm.
    /// Algorithm:
    ///     ReconfigureList = all child activities
    ///     While (ReconfigureList is not empty) (i.e. for each child activity in the list):
    ///         If (Child activity does not exist):
    ///             CreateNewChildActivity
    ///         Else
    ///             ReconfigureChildActivity
    ///             
    ///         If (a downstream activity is dependend on current activity):
    ///             Add downstream activity to processing queue
    ///         
    ///         RemoveActivityFromList
    /// End of Algorithm.
    /// 
    /// ReconfigurationListBehavior targets scenarios, whens solution pre-configures child activities from different terminals which require authentication.
    /// As an example we can take a look at Mail_Merge solution, when user adds GoogleSheet and Send_DocuSign_Envelope activities,
    /// and both Google and DocuSign terminal to not have default auth-tokens.
    /// In such case Hub asks user to perform authentication, assigns auth-tokens to child activities, and reconfigures the entire solution again.
    /// ReconfigurationList helps to structure the code to ease entire solution reconfiguration scenarios.
    /// See https://maginot.atlassian.net/browse/FR-2488 for more details.
    /// </summary>
    public class ReconfigurationListBehavior
    {
        /// <summary>
        /// ReconfigurationList algorithm.
        /// </summary>
        public async Task ReconfigureActivities(ActivityPayload solution,
            AuthorizationToken authToken, IReadOnlyList<ConfigurationRequest> items)
        {
            var queue = new Queue<ConfigurationRequest>(items);

            if (solution.ChildrenActivities == null)
            {
                solution.ChildrenActivities = new List<ActivityPayload>();
            }

            while (queue.Count > 0)
            {
                var item = queue.Dequeue();

                var context = new ReconfigurationContext()
                {
                    SolutionActivity = solution,
                    AuthToken = authToken,
                    Requests = items
                };

                if (!await item.HasActivityMethod(context))
                {
                    var childActivityByIndex = solution.ChildrenActivities
                        .SingleOrDefault(x => x.Ordering == item.ChildActivityIndex);

                    if (childActivityByIndex != null)
                    {
                        solution.ChildrenActivities.Remove(childActivityByIndex);
                    }
                    await item.CreateActivityMethod(context);
                }
                else
                {
                    await item.ConfigureActivityMethod(context);
                }

                if (context.AdditionalRequests.Count > 0)
                {
                    foreach (var additionalItem in context.AdditionalRequests)
                    {
                        if (queue.All(x => x.ChildActivityIndex != additionalItem.ChildActivityIndex))
                        {
                            queue.Enqueue(additionalItem);
                        }
                    }
                }
            }
        }
    }
}
