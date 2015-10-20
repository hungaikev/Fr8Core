using System.Collections.Generic;
using System.Web.Http;
using Data.Entities;
using Data.States;
using System.Web.Http.Description;
namespace pluginTwilio.Controllers
{    
    [RoutePrefix("plugins")]
    public class PluginController : ApiController
    {
        [HttpGet]
        [Route("discover")]
        [ResponseType(typeof(List<ActivityTemplateDO>))]
        public IHttpActionResult DiscoverPlugins()
        {
            var plugin = new PluginDO()
            {
                Name = "pluginTwilio",
                PluginStatus = PluginStatus.Active,
                Endpoint = "localhost:30699",
                Version = "1"
            };

            var sendViaTwilioTemplate = new ActivityTemplateDO
            {
                Name = "Send_Via_Twilio",
                Label = "Send Via Twilio",
                Category = ActivityCategory.Forwarders,
                Version = "1",
                Plugin = plugin,
                MinPaneWidth = 330
            };

            var actionList = new List<ActivityTemplateDO>
            {
                sendViaTwilioTemplate
            };

            return Json(actionList);   
        }
    }
}