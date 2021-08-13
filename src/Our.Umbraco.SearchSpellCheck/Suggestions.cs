using System.IO;
using System.Linq;
using Umbraco.Core.IO;
using Lucene.Net.Store;
using Lucene.Net.Index;
using SpellChecker.Net.Search.Spell;
using System.Configuration;

namespace Our.Umbraco.SearchSpellCheck
{
    public static class Suggestions
    {
        public static string GetSuggestion(string searchTerm, int numberOfSuggestions = 10)
        {
            var suggestions = GetSuggestions(searchTerm, numberOfSuggestions);
            return suggestions.Select(x => x.Word).FirstOrDefault();
        }

        public static IOrderedEnumerable<Suggestion> GetSuggestions(string searchTerm, int numberOfSuggestions = 10)
        {
            string indexName = ConfigurationManager.AppSettings[Constants.Configuration.IndexName];

            var indexReader = IndexReader.Open(GetFileSystemLuceneDirectory(indexName), true);

            var jaro = new JaroWinklerDistance();
            var leven = new LevenshteinDistance();
            var ngram = new NGramDistance();

            var checker = new SpellChecker.Net.Search.Spell.SpellChecker(new RAMDirectory(), jaro);
            checker.IndexDictionary(new LuceneDictionary(indexReader, Constants.Internals.FieldName));

            var suggestions = checker.SuggestSimilar(searchTerm, numberOfSuggestions, indexReader, Constants.Internals.FieldName, true);

            var metrics = suggestions.Select(s => new Suggestion
            {
                Word = s,
                Frequency = indexReader.DocFreq(new Term(Constants.Internals.FieldName, s)),
                Jaro = jaro.GetDistance(searchTerm, s),
                Leven = leven.GetDistance(searchTerm, s),
                NGram = ngram.GetDistance(searchTerm, s)
            })
            .OrderByDescending(metric => Priority(metric));

            return metrics;
        }

        internal static float Priority(Suggestion metric)
        {
            return ((metric.Frequency / 100f) + metric.Jaro + metric.Leven + metric.NGram) / 4f;
        }

        internal static Lucene.Net.Store.Directory GetFileSystemLuceneDirectory(string indexName)
        {
            var dirInfo = new DirectoryInfo(Path.Combine(IOHelper.MapPath(SystemDirectories.TempData), "ExamineIndexes", indexName));
            var luceneDir = new SimpleFSDirectory(dirInfo);
            return luceneDir;
        }
    }
}