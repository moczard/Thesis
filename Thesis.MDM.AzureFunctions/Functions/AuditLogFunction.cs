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
    public static class AuditLogFunction
    {
        private static string domain = ConfigurationManager.AppSettings["domain"];
        private static string servicePrincipalId = ConfigurationManager.AppSettings["servicePrincipalId"];
        private static string servicePrincipalKey = ConfigurationManager.AppSettings["servicePrincipalKey"];
        private static string adlAccountName = ConfigurationManager.AppSettings["adlAccountName"];
        private static string logFilePath = ConfigurationManager.AppSettings["logFilePath"];

        [FunctionName("AuditLogFunction")]
        public static void Run([ServiceBusTrigger("logs", "audit-log", AccessRights.Listen, Connection = "ServiceBusConnectionString")]BrokeredMessage message, TraceWriter log)
        {
            var messageId = message.MessageId;
            var messageBody = new StreamReader(message.GetBody<Stream>(), Encoding.UTF8);

            try
            {
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
                var clientCredential = new ClientCredential(servicePrincipalId, servicePrincipalKey);
                var creds = ApplicationTokenProvider.LoginSilentAsync(domain, clientCredential).Result;

                var adlsFileSystemClient = new DataLakeStoreFileSystemManagementClient(creds);

                adlsFileSystemClient.FileSystem.ConcurrentAppend(adlAccountName, logFilePath, messageBody.BaseStream, appendMode: AppendModeType.Autocreate);
                log.Info($"The message has been sent to the data lake messageId: {messageId}", "DATA_LAKE_AUDITLOG");
            }
            catch (Exception e)
            {
                log.Error("There was an error processing the message: ", e, "DATA_LAKE_ERROR");
            }
        }
    }
}
