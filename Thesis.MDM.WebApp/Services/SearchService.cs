using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using System.Configuration;
using System.Threading.Tasks;
using Thesis.MDM.WebApplication.Models;

namespace Thesis.MDM.WebApplication.Services
{
    public static class SearchService
    {
        private static string SearchServiceName = ConfigurationManager.AppSettings["searchServiceName"];
        private static string AdminApiKey = ConfigurationManager.AppSettings["adminApiKey"];
        private static string IndexName = ConfigurationManager.AppSettings["indexName"];

        private static SearchIndexClient searchIndexClient;

        static SearchService()
        {
            searchIndexClient = new SearchIndexClient(SearchServiceName, IndexName, new SearchCredentials(AdminApiKey));
        }

        public static async Task<DocumentSearchResult<Person>> SearchAsync(string searchText, SearchRequestOptions searchRequestOptions = null)
        {
            var searchParameters = new SearchParameters
            {
                Top = 2,
                QueryType = QueryType.Full
            };

            searchText = searchText.Replace('[', '\0');
            searchText = searchText.Replace(']', '\0');

            return await searchIndexClient.Documents.SearchAsync<Person>(searchText, searchParameters, searchRequestOptions);
        }

        public static async Task<Person> GetAsync(string key)
        {
            return await searchIndexClient.Documents.GetAsync<Person>(key);
        }

        public static async Task<DocumentSuggestResult<Person>> SuggestAsync(string searchText, bool fuzzy)
        {

            SuggestParameters parameters = new SuggestParameters()
            {
                UseFuzzyMatching = fuzzy,
                Top = 5
            };

            return await searchIndexClient.Documents.SuggestAsync<Person>(searchText, "sg", parameters);
        }        
    }
}