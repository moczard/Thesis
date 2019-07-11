using Microsoft.Analytics.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DataLakeAnalytics.ClassLibrary.Extractors
{
    public class JsonExtractor : IExtractor
    {
        public override IEnumerable<IRow> Extract(IUnstructuredReader input, IUpdatableRow output)
        {
            string line;

            using (StreamReader streamReader = new StreamReader(input.BaseStream, Encoding.UTF8))
            {
                while ((line = streamReader.ReadLine()) != null)
                {
                    var jObject = JsonConvert.DeserializeObject<JObject>(line);
                    foreach (var column in output.Schema)
                    {
                        if(column.Type == typeof(string))
                        {
                            output.Set(column.Name, jObject[column.Name].ToString());
                        }
                        if(column.Type == typeof(DateTime))
                        {
                            output.Set(column.Name, (DateTime.Parse(jObject[column.Name].ToString())));
                        }
                    }

                    yield return output.AsReadOnly();
                }

            }

            yield break;
        }
    }
}
