using Microsoft.Analytics.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DataLakeAnalytics.ClassLibrary.Extractors
{
    public class MergeExtractor : IExtractor
    {
        public override IEnumerable<IRow> Extract(IUnstructuredReader input, IUpdatableRow output)
        {
            string line;

            using (StreamReader streamReader = new StreamReader(input.BaseStream, Encoding.UTF8))
            {
                while ((line = streamReader.ReadLine()) != null)
                {
                    var jObject = JsonConvert.DeserializeObject<JObject>(line);
                    var mergeType = jObject["MergeType"].ToString();

                    if (mergeType == "Merge")
                    {
                        var mergedPerson = jObject["MergedPerson"];

                        foreach (var column in output.Schema)
                        {
                            if (column.Type == typeof(DateTime))
                            {
                                output.Set(column.Name, (DateTime.Parse(jObject[column.Name].ToString())));
                            }
                            else
                            {
                                output.Set(column.Name, mergedPerson[column.Name].ToString());
                            }
                        }
                        yield return output.AsReadOnly();
                    }
                    else
                    {
                        var person1 = jObject["Person1"];
                        var person2 = jObject["Person2"];

                        foreach (var column in output.Schema)
                        {
                            if (column.Type == typeof(DateTime))
                            {
                                output.Set(column.Name, (DateTime.Parse(jObject[column.Name].ToString())));
                            }
                            else
                            {
                                output.Set(column.Name, person1[column.Name].ToString());
                            }
                        }

                        yield return output.AsReadOnly();

                        foreach (var column in output.Schema)
                        {
                            if (column.Type == typeof(DateTime))
                            {
                                output.Set(column.Name, (DateTime.Parse(jObject[column.Name].ToString())));
                            }
                            else
                            {
                                output.Set(column.Name, person2[column.Name].ToString());
                            }
                        }

                        yield return output.AsReadOnly();
                    }
                }

            }

            yield break;
        }
    }
}
