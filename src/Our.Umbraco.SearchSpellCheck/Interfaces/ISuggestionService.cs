using System.Linq;
using Lucene.Net.Store;

namespace Our.Umbraco.SearchSpellCheck.Interfaces
{
    public interface ISuggestionService
    {
        string GetSuggestion(string searchTerm, int numberOfSuggestions = 10, float suggestionAccuracy = 0.75f, string culture = null);
        IOrderedEnumerable<Suggestion> SuggestionData(string word, int numberOfSuggestions = 10, string culture = null);
        float? Priority(Suggestion metric);
        SimpleFSDirectory GetFileSystemLuceneDirectory(string indexName);
    }
}