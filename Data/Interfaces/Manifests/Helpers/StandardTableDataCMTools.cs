﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Crates;
using Data.Interfaces.DataTransferObjects;
using Newtonsoft.Json.Linq;
using StructureMap;

namespace Data.Interfaces.Manifests.Helpers
{
    public class StandardTableDataCMTools
    {
        public static StandardTableDataCM ExtractPayloadCrateDataToStandardTableData(ICrateStorageSerializer storageSerializer, IEnumerable<Crate> crates)
        {
            var tableData = new StandardTableDataCM()
            {
                FirstRowHeaders = true,
                Table = new List<TableRowDTO>()
            };
            var headerIsAdded = false;
            foreach (var crate in crates)
            {
                Type crateManifestType;
                ManifestDiscovery.Default.TryResolveType(crate.ManifestType, out crateManifestType);

                if (crateManifestType == typeof(StandardTableDataCM))
                {
                    //add to the table data this existing standard table data
                    //check if column headers are the same
                    tableData = crate.Get<StandardTableDataCM>();
                    continue;
                }

                var item = storageSerializer.ConvertToDto(crate);

                var token = JToken.Parse(item.Contents.ToString());

                var jObject = token as JObject;
                if (jObject != null)
                {
                    //check if jObject has some JArray properties
                    var arrayProperty = jObject.Properties().FirstOrDefault(x => x.Value is JArray);

                    //check how StandardPayloadDataCM is structured
                    if (arrayProperty != null)
                    {
                        foreach (var arrayItem in arrayProperty.Value)
                        {
                            //arrayItem is PayloadObjectDTO which on has an List<FieldDTO>
                            var innerArrayProperty = ((JObject)arrayItem).Properties().FirstOrDefault(x => x.Value is JArray);
                            if (innerArrayProperty != null)
                            {
                                var headerRow = new TableRowDTO();
                                var dataRow = new TableRowDTO();

                                foreach (var innerArrayItem in innerArrayProperty.Value)
                                {
                                    //try to parse the property as FieldDTO
                                    if (innerArrayItem is JObject)
                                    {
                                        var fieldObj = (JObject)innerArrayItem;
                                        if (fieldObj.Property("key") != null && fieldObj.Property("value") != null)
                                        {
                                            headerRow.Row.Add(new TableCellDTO() { Cell = new FieldDTO(fieldObj["key"].ToString(), fieldObj["key"].ToString()) });
                                            dataRow.Row.Add(new TableCellDTO() { Cell = new FieldDTO(fieldObj["key"].ToString(), fieldObj["value"].ToString()) });
                                        }
                                    }
                                }

                                if (!headerIsAdded)
                                {
                                    tableData.Table.Add(headerRow);
                                    headerIsAdded = true;
                                }
                                tableData.Table.Add(dataRow);
                            }
                        }
                    }
                    else
                    {
                        var headerRow = new TableRowDTO();
                        var dataRow = new TableRowDTO();

                        foreach (JProperty property in jObject.Properties())
                        {
                            //try to parse the property as FieldDTO
                            if (property.Value is JObject)
                            {
                                var fieldObj = (JObject)property.Value;
                                if (fieldObj.Property("key") != null && fieldObj.Property("value") != null)
                                {
                                    headerRow.Row.Add(new TableCellDTO() { Cell = new FieldDTO(fieldObj["key"].ToString(), fieldObj["key"].ToString()) });
                                    dataRow.Row.Add(new TableCellDTO() { Cell = new FieldDTO(fieldObj["key"].ToString(), fieldObj["value"].ToString()) });
                                }
                            }
                            else
                            {
                                headerRow.Row.Add(new TableCellDTO() { Cell = new FieldDTO(property.Name, property.Name) });
                                dataRow.Row.Add(new TableCellDTO() { Cell = new FieldDTO(property.Name, property.Value.ToString()) });
                            }
                        }

                        if (!headerIsAdded)
                        {
                            tableData.Table.Add(headerRow);
                            headerIsAdded = true;
                        }
                        tableData.Table.Add(dataRow);
                    }
                }
            }

            return tableData;
        }
    }
}
