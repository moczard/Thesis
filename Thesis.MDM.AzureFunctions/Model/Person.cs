using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

namespace Thesis.MDM.AzureFunctions.Model
{
    public class Person
    {
        [IsSearchable]
        [System.ComponentModel.DataAnnotations.Key]        
        public string Id { get; set; }

        [IsSearchable, IsFilterable, IsSortable]
        [Analyzer(AnalyzerName.AsString.EnMicrosoft)] 
        
        public string FirstName { get; set; }

        [IsSearchable, IsFilterable, IsSortable]
        [Analyzer(AnalyzerName.AsString.EnMicrosoft)]
        public string LastName { get; set; }

        [IsSearchable, IsFilterable, IsSortable]
        [Analyzer(AnalyzerName.AsString.EnMicrosoft)]
        public string Email { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsFacetable]
        [Analyzer(AnalyzerName.AsString.EnMicrosoft)]
        public string Gender { get; set; }

        [IsSearchable, IsFilterable, IsSortable]
        [Analyzer(AnalyzerName.AsString.EnMicrosoft)]
        public string City { get; set; }

        [IsSearchable, IsFilterable, IsSortable]
        [Analyzer(AnalyzerName.AsString.EnMicrosoft)]
        public string Country { get; set; }

        [IsSearchable, IsFilterable]
        [Analyzer(AnalyzerName.AsString.EnMicrosoft)]
        public string StreetAddress { get; set; }

        [IsSearchable, IsFilterable, IsSortable]
        [Analyzer(AnalyzerName.AsString.EnMicrosoft)]
        public string CompanyName { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsFacetable]
        [Analyzer(AnalyzerName.AsString.EnMicrosoft)]
        public string JobTitle { get; set; }

        [IsSearchable, IsFilterable, IsSortable]
        [Analyzer(AnalyzerName.AsString.EnMicrosoft)]
        public string PhoneNumber { get; set; }
    }
}
