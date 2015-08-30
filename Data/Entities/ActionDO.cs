﻿using Data.States.Templates;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Data.Wrappers;

namespace Data.Entities
{
	public class ActionDO: ActivityDO
	{
        public string UserLabel{ get; set; }

        public string ActionType { get; set; }

        [ForeignKey("ActionList")]
        public int? ActionListId { get; set; }
        public virtual ActionListDO ActionList { get; set; }

        public string ConfigurationSettings { get; set; }

        public string FieldMappingSettings { get; set; }

        public string ParentPluginRegistration { get; set; }

        [ForeignKey("ActionStateTemplate")]
        public int? ActionState { get; set; }

        public virtual _ActionStateTemplate ActionStateTemplate { get; set; }

        public string PayloadMappings { get; set; }

        [NotMapped]
	    public string DocuSignTemplateId { get; set; }
	}
}