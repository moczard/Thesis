using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Thesis.MDM.WebApp.Services
{
    public class LogService
    {
        private static string ConnectionString = ConfigurationManager.AppSettings["connectionString"];
        private static string TopicName = ConfigurationManager.AppSettings["logsTopicName"];

        private static TopicClient client;

        static LogService()
        {
            client = TopicClient.CreateFromConnectionString(ConnectionString, TopicName);
        }

        public static async Task SendLogAsync(string action, string user, object details = null)
        {
            var messageObject = new
            {
                Action = action,
                User = user,
                Timestamp = DateTime.Now,
                Details = details
            };

            var message = JsonConvert.SerializeObject(messageObject);
            message = message + "\n";

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(message));

            var brokeredMessage = new BrokeredMessage(stream, true)
            {
                CorrelationId = Guid.NewGuid().ToString()
            };

            await client.SendAsync(brokeredMessage);
        }       
    }
}