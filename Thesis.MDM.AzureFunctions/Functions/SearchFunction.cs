using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.IO;
using System.Text;
using Thesis.MDM.AzureFunctions.Model;

namespace Thesis.MDM.AzureFunctions.Functions
{
    public static class SearchFunction
    {
        private static string searchServiceName = ConfigurationManager.AppSettings["searchServiceName"];
        private static string adminApiKey = ConfigurationManager.AppSettings["adminApiKey"];
        private static string indexName = ConfigurationManager.AppSettings["indexName"];

        private static SearchIndexClient IndexClient;

        [FunctionName("SearchFunction")]
        public static void Run(
        [ServiceBusTrigger("events", "azure-search", AccessRights.Listen, Connection = "ServiceBusConnectionString")]
        BrokeredMessage message,
        TraceWriter log)
        {
            var messageId = message.MessageId;
            var messageBody = new StreamReader(message.GetBody<Stream>(), Encoding.UTF8);

            try
            {
                IndexClient = new SearchIndexClient(searchServiceName, indexName, new SearchCredentials(adminApiKey));

                if (message.Properties["merge"].ToString() == "false")
                {
                    Person[] people = new Person[] { JsonConvert.DeserializeObject<Person>(messageBody.ReadToEnd()) };
                    UploadIndexBatch(people);
                    log.Info($"The index upload completed succesfully. MessageId: {messageId}", "AZURE_SEARCH_UPDATE");
                }
                else if (message.Properties["merge"].ToString() == "true")
                {
                    var response = JsonConvert.DeserializeObject<JObject>(messageBody.ReadToEnd());
                    var mergeType = response["MergeType"].ToString();

                    if (mergeType == "Merge")
                    {
                        var mergedPersonJson = response["MergedPerson"].ToString();
                        var id1 = response["Person1"]["Id"].ToString();
                        var id2 = response["Person2"]["Id"].ToString();
                        Delete(id1);
                        Delete(id2);

                        Person[] people = new Person[] { JsonConvert.DeserializeObject<Person>(mergedPersonJson) };
                        UploadIndexBatch(people);
                        log.Info($"The index upload completed succesfully. MessageId: {messageId}", "AZURE_SEARCH_MERGE");
                    }

                    if (mergeType == "Unmerge")
                    {
                        var person1 = response["Person1"].ToString();
                        var person2 = response["Person2"].ToString();
                        var id = response["MergedPerson"]["Id"].ToString();
                        Delete(id);

                        Person[] people = new Person[] { JsonConvert.DeserializeObject<Person>(person1), JsonConvert.DeserializeObject<Person>(person2) };
                        UploadIndexBatch(people);
                        log.Info($"The index upload completed succesfully. MessageId: {messageId}", "AZURE_SEARCH_UNMERGE");
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("There was an error processing the message: ", e, "AZURE_SEARCH_ERROR");
            }
        }

        private static void UploadIndexBatch(Person[] people)
        {
            var batch = IndexBatch.Upload(people);
            IndexClient.Documents.Index(batch);
        }

        public static void Delete(string id)
        {
            Person[] people = new Person[1];

            var person = IndexClient.Documents.Get<Person>(id);
            if (person != null)
            {
                people[0] = person;
                var batch = IndexBatch.Delete(people);
                IndexClient.Documents.Index(batch);
            }
        }
    }
}
