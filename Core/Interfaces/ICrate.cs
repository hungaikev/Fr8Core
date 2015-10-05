﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.ManifestSchemas;

namespace Core.Interfaces
{
    public interface ICrate
    {
        CrateDTO Create(string label, string contents, string manifestType = "", int manifestId = 0);
        T GetContents<T>(CrateDTO crate);
        IEnumerable<JObject> GetElementByKey<TKey>(IEnumerable<CrateDTO> searchCrates, TKey key, string keyFieldName);
        CrateDTO CreateAuthenticationCrate(string label, AuthenticationMode mode, string url = null);
        CrateDTO CreateDesignTimeFieldsCrate(string label, params FieldDTO[] fields);
        CrateDTO CreateStandardConfigurationControlsCrate(string label, params ControlsDefinitionDTO[] controls);
        CrateDTO CreateStandardEventSubscriptionsCrate(string label, params string[] subscriptions);
        CrateDTO CreateStandardTableDataCrate(string label, bool firstRowHeaders, params TableRowDTO[] table);

        void RemoveCrateByManifestId(IList<CrateDTO> crates, int manifestId);
        void RemoveCrateByManifestType(IList<CrateDTO> crates, string manifestType);
        void RemoveCrateByLabel(IList<CrateDTO> crates, string label);
    }
}
