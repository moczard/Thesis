using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Data.SqlClient;
using Microsoft.Azure.Search;
using System;
using System.IO;
using System.Text;
using System.Configuration;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents;
using Newtonsoft.Json.Linq;
using Microsoft.Azure.Search.Models;
using System.Collections.Generic;
using Thesis.MDM.AzureFunctions.Model;

namespace Thesis.MDM.AzureFunctions.Functions
{
    public static class DatabaseLoaderFunction
    {
        private static readonly string dataSource = ConfigurationManager.AppSettings["dataSource"];
        private static readonly string userID = ConfigurationManager.AppSettings["userID"];
        private static readonly string password = ConfigurationManager.AppSettings["password"];
        private static readonly string initialCatalog = ConfigurationManager.AppSettings["initialCatalog"];
        private static readonly string initialTable = ConfigurationManager.AppSettings["sqlInitialTableName"];

        private static string searchServiceName = ConfigurationManager.AppSettings["searchServiceName"];
        private static string adminApiKey = ConfigurationManager.AppSettings["adminApiKey"];
        private static string indexName = ConfigurationManager.AppSettings["indexName"];

        private static readonly string databaseId = ConfigurationManager.AppSettings["database"];
        private static readonly string collectionId = ConfigurationManager.AppSettings["collection"];
        private static readonly string endpoint = ConfigurationManager.AppSettings["endpoint"];
        private static readonly string authKey = ConfigurationManager.AppSettings["authKey"];
        private static DocumentClient client;

        [FunctionName("DatabaseLoaderFunction")]
        public static async Task Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            // Upload data to the sql database
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
                {
                    DataSource = dataSource,
                    UserID = userID,
                    Password = password,
                    InitialCatalog = initialCatalog
                };

                await UploadDataToSqlDbAsync(builder);
            }
            catch (SqlException e)
            {
                log.Error("There was an error uploading the data to the sql database", e);
            }

            //Upload data to the DocumentDb
            try
            {
                client = new DocumentClient(new Uri(endpoint), authKey);
                await CreateDatabaseIfNotExistsAsync();
                await CreateCollectionIfNotExistsAsync();
                await CreateItemAsync();
            }
            catch (Exception e)
            {
                log.Error("There was an error uploading the data to the documentdb", e);
            }

            // Create index
            try
            {
                SearchServiceClient serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(adminApiKey));

                var definition = new Microsoft.Azure.Search.Models.Index()
                {
                    Name = indexName,
                    Fields = FieldBuilder.BuildForType<Person>(),
                    Suggesters = new List<Suggester>()
                    {
                        new Suggester("sg", SuggesterSearchMode.AnalyzingInfixMatching, "FirstName", "LastName")
                    }
                };

                await serviceClient.Indexes.CreateOrUpdateAsync(definition);


            }
            catch (Exception e)
            {
                log.Error("There was an error creating the index", e);
            }
        }

        private static async Task UploadDataToSqlDbAsync(SqlConnectionStringBuilder builder)
        {
            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                connection.Open();
                StringBuilder sb = new StringBuilder();
                var location = new StringBuilder();
                location.Append(Environment.GetEnvironmentVariable("HOME"));
                location.Append(@"\site\wwwroot\Data\SqlData.csv");
                using (StreamReader sr = new StreamReader(location.ToString(), Encoding.UTF8))
                {
                    var header = sr.ReadLine();
                    var columns = header.Split(',');
                    sb.Append("CREATE TABLE " + initialTable + " (");
                    foreach (var column in columns)
                    {
                        sb.Append($"{column} varchar(255),");
                    }
                    sb.Remove(sb.Length - 1, 1);
                    sb.Append(");");

                    sb.Append("INSERT INTO " + initialTable + " VALUES");
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        line = line.Replace("'", "''");
                        sb.Append("(");
                        var quotationmark = false;
                        foreach (var data in line.Split(','))
                        {
                            if (data.Contains("\"") && !quotationmark)
                            {
                                var dataWithoutQuotationMark = data.Replace("\"", String.Empty);
                                quotationmark = !quotationmark;
                                sb.Append($"'{dataWithoutQuotationMark},");
                            }
                            else if (quotationmark)
                            {
                                if (data.Contains("\""))
                                {
                                    var dataWithoutQuotationMark = data.Replace("\"", String.Empty);
                                    sb.Append($"{dataWithoutQuotationMark}',");
                                    quotationmark = !quotationmark;
                                }
                                else
                                {
                                    sb.Append($"{data},");
                                }
                            }
                            else
                            {
                                sb.Append($"'{data}',");
                            }
                        }
                        sb.Remove(sb.Length - 1, 1);
                        sb.Append("),\n");
                    }
                    sb.Remove(sb.Length - 2, 1);
                    sb.Append(";");

                }

                String sql = sb.ToString();
                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }

            }
        }

        private static async Task CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                await client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(databaseId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await client.CreateDatabaseAsync(new Database { Id = databaseId });
                }
                else
                {
                    throw;
                }
            }
        }

        private static async Task CreateCollectionIfNotExistsAsync()
        {
            try
            {
                await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(databaseId, collectionId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(databaseId),
                        new DocumentCollection { Id = collectionId },
                        new RequestOptions { OfferThroughput = 400 });
                }
                else
                {
                    throw;
                }
            }
        }

        public static async Task CreateItemAsync()
        {
            var location = new StringBuilder();
            location.Append(Environment.GetEnvironmentVariable("HOME"));
            location.Append(@"\site\wwwroot\Data\DocumentDbUnmappedData.json");
            using (var reader = new StreamReader(location.ToString()))
            {
                var item = reader.ReadToEnd();
                JArray jArray = JArray.Parse(item);
                foreach (var jObject in jArray)
                {
                    await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseId, collectionId), jObject);
                }
            }
        }
    }
}
