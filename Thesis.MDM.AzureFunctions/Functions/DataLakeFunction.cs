using Microsoft.Azure.Management.DataLake.Store;
using Microsoft.Azure.Management.DataLake.Store.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Rest.Azure.Authentication;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading;

namespace Thesis.MDM.AzureFunctions.Functions
{
    public static class DataLakeFunction
    {
        private static string domain = ConfigurationManager.AppSettings["domain"];
        private static string servicePrincipalId = ConfigurationManager.AppSettings["servicePrincipalId"];
        private static string servicePrincipalKey = ConfigurationManager.AppSettings["servicePrincipalKey"];
        private static string adlAccountName = ConfigurationManager.AppSettings["adlAccountName"];
        private static string updateFilePath = ConfigurationManager.AppSettings["updateFilePath"];
        private static string mergeFilePath = ConfigurationManager.AppSettings["mergeFilePath"];

        [FunctionName("DataLakeFunction")]
        public static void Run([ServiceBusTrigger("events", "data-lake", AccessRights.Listen, Connection = "ServiceBusConnectionString")] BrokeredMessage message, TraceWriter log)
        {
            var messageId = message.MessageId;
            var messageBody = new StreamReader(message.GetBody<Stream>(), Encoding.UTF8);

            try
            {
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                var clientCredential = new ClientCredential(servicePrincipalId, servicePrincipalKey);
                var creds = ApplicationTokenProvider.LoginSilentAsync(domain, clientCredential).Result;

                var adlsFileSystemClient = new DataLakeStoreFileSystemManagementClient(creds);

                if(message.Properties["merge"].ToString() == "false")
                {
                    adlsFileSystemClient.FileSystem.ConcurrentAppend(adlAccountName, updateFilePath, messageBody.BaseStream, appendMode: AppendModeType.Autocreate);
                    log.Info($"The message has been sent to the data lake messageId: {messageId}", "DATA_LAKE_UPDATE");

                }
                else if (message.Properties["merge"].ToString() == "true")
                {                    
                    adlsFileSystemClient.FileSystem.ConcurrentAppend(adlAccountName, mergeFilePath, messageBody.BaseStream, appendMode: AppendModeType.Autocreate);
                    log.Info($"The message has been sent to the data lake messageId: {messageId}", "DATA_LAKE_(UN)MERGE");
                }

            }
            catch (Exception e)
            {
                log.Error("There was an error processing the message: ", e, "DATA_LAKE_ERROR");
            }
        }
    }
}
