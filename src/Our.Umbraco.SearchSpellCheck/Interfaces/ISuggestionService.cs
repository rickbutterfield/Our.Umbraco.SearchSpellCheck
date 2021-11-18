#if NETCOREAPP
using System.Linq;
using Lucene.Net.Store;

namespace Our.Umbraco.SearchSpellCheck.Interfaces
{
    public interface ISuggestionService
    {
        string GetSuggestion(string searchTerm, int numberOfSuggestions = 10, float accuracy = 0.75f);
        IOrderedEnumerable<Suggestion> SuggestionData(string word, int numberOfSuggestions = 10, float searchAccuracy = 1f);
        float? Priority(Suggestion metric);
        SimpleFSDirectory GetFileSystemLuceneDirectory(string indexName);
    }
}
#endif