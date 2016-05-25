﻿using System;
using System.Threading.Tasks;
using StructureMap;
using System.Web.Mvc;
using Hub.Interfaces;
using Hub.Managers;
using Utilities;

namespace HubWeb.Controllers
{
    public class RedirectController : Controller
    {
        private readonly IPlan _plan;

        public RedirectController()
        {
            _plan = ObjectFactory.GetInstance<IPlan>();
        }

        public ActionResult TwilioSMS()
        {
            var configRepository = ObjectFactory.GetInstance<IConfigRepository>();

            string smsURL = configRepository.Get("DocumentationFr8Site_SMSLink");
            return Redirect(smsURL);
        }


        [HttpGet]
        [DockyardAuthorize]
        public async Task<ActionResult> ClonePlan(Guid id)
        {
            //let's clone the plan and redirect user to that cloned plan url
            var clonedPlan = await _plan.Clone(id);
            var baseUri = Request.Url.GetLeftPart(UriPartial.Authority);
            var clonedPlanUrl = baseUri + "/dashboard/plans/" + clonedPlan.Id + "/builder?viewMode=kiosk&view=Collection";
            return Redirect(clonedPlanUrl);
        }
    }
}