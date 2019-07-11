using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Thesis.MDM.WebApplication.Models;

namespace Thesis.MDM.WebApplication.Services
{
    public static class EventService
    {
        private static string ConnectionString = ConfigurationManager.AppSettings["connectionString"];
        private static string TopicName = ConfigurationManager.AppSettings["eventsTopicName"];

        private static TopicClient client;

        static EventService()
        {
            client = TopicClient.CreateFromConnectionString(ConnectionString, TopicName);
        }

        public static async Task SendUpdateAsync(Person person, string user)
        {
            JObject jObject = JObject.FromObject(person);
            jObject.Add("User", user);
            jObject.Add("Timestamp", DateTime.Now);

            var message = JsonConvert.SerializeObject(jObject);
            message = message + "\n";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(message));

            var brokeredMessage = new BrokeredMessage(stream, true)
            {
                CorrelationId = Guid.NewGuid().ToString()
            };

            brokeredMessage.Properties.Add("merge", "false");

            await client.SendAsync(brokeredMessage);
        }

        public static async Task SendMergeAsync(Person person1, Person person2, Person mergedPerson, string user, bool unmerge)
        {
            var messageObject = new
            {
                MergeType = unmerge ? "Unmerge" : "Merge",
                Person1 = person1,
                Person2 = person2,
                MergedPerson = mergedPerson,
                User = user,
                Timestamp = DateTime.Now
            };

            var message = JsonConvert.SerializeObject(messageObject);
            message = message + "\n";

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(message));

            var brokeredMessage = new BrokeredMessage(stream, true)
            {
                CorrelationId = Guid.NewGuid().ToString()
            };

            brokeredMessage.Properties.Add("merge", "true");          

            await client.SendAsync(brokeredMessage);
        }
    }
}