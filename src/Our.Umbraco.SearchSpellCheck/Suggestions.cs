#if NETCOREAPP
using Umbraco.Cms.Core.IO;
using Lucene.Net.Search.Spell;
using Microsoft.Extensions.Hosting;
using static Umbraco.Cms.Core.Constants;
using Umbraco.Cms.Core.Configuration.Models;
#else
using Umbraco.Core.IO;
using System.Configuration;
using SpellChecker.Net.Search.Spell;
#endif
using System.IO;
using System.Linq;
using Lucene.Net.Store;
using Lucene.Net.Index;

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
            string indexName = string.Empty;

#if NETCOREAPP
#else
            indexName = ConfigurationManager.AppSettings[Constants.Configuration.IndexName];
#endif
            var luceneDirectory = GetFileSystemLuceneDirectory(indexName);

#if NETCOREAPP
            var indexReader = DirectoryReader.Open(luceneDirectory);
#else
            var indexReader = IndexReader.Open(luceneDirectory, true);
#endif

            var jaro = new JaroWinklerDistance();
#if NETCOREAPP
            var leven = new LuceneLevenshteinDistance();
#else
            var leven = new LevenshteinDistance();
#endif
            var ngram = new NGramDistance();

#if NETCOREAPP
            var checker = new SpellChecker(new RAMDirectory(), jaro);
            var suggestions = checker.SuggestSimilar(searchTerm, numberOfSuggestions, indexReader, Constants.Internals.FieldName, SuggestMode.SUGGEST_ALWAYS, 0.5f);
#else
            var checker = new SpellChecker.Net.Search.Spell.SpellChecker(new RAMDirectory(), jaro);
            checker.IndexDictionary(new LuceneDictionary(indexReader, Constants.Internals.FieldName));
            var suggestions = checker.SuggestSimilar(searchTerm, numberOfSuggestions, indexReader, Constants.Internals.FieldName, true);
#endif

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
#if NETCOREAPP
            var dirInfo = new DirectoryInfo(Path.Combine(SystemDirectories.TempData, "ExamineIndexes", indexName));
#else
            var path = IOHelper.MapPath(SystemDirectories.TempData);
            var dirInfo = new DirectoryInfo(Path.Combine(IOHelper.MapPath(SystemDirectories.TempData), "ExamineIndexes", indexName));
#endif
            var luceneDir = new SimpleFSDirectory(dirInfo);
            return luceneDir;
        }
    }
}