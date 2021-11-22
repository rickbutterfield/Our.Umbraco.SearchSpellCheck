#if !NETCOREAPP
using Umbraco.Core.IO;
using System.Configuration;
using SpellChecker.Net.Search.Spell;
using System.IO;
using System.Linq;
using Lucene.Net.Store;
using Lucene.Net.Index;
using System.Collections.Generic;

namespace Our.Umbraco.SearchSpellCheck
{
    public static class Suggestions
    {
        public static string GetSuggestion(string searchTerm, int numberOfSuggestions = 10, float accuracy = 0.75f)
        {
            var words = searchTerm.Split(' ');
            var suggestions = new List<string>();
            foreach (string word in words)
            {
                var suggest = SuggestionData(word, numberOfSuggestions);
                if (suggest != null)
                {
                    var first = suggest.FirstOrDefault();
                    if (first != null)
                    {
                        if (first.Priority > accuracy)
                        {
                            suggestions.Add(first.Word);
                        }
                        else
                        {
                            suggestions.Add(word);
                        }
                    }
                }
            }

            return string.Join(" ", suggestions);
        }

        internal static IOrderedEnumerable<Suggestion> SuggestionData(string word, int numberOfSuggestions = 10, float searchAccuracy = 1f)
        {
            string indexName = ConfigurationManager.AppSettings[Constants.Configuration.IndexName] ?? Constants.Configuration.DefaultIndexName;

            var indexReader = IndexReader.Open(GetFileSystemLuceneDirectory(indexName), true);

            var jaro = new JaroWinklerDistance();
            var leven = new LevenshteinDistance();
            var ngram = new NGramDistance();

            var checker = new SpellChecker.Net.Search.Spell.SpellChecker(new RAMDirectory(), jaro);
            var dictionary = new LuceneDictionary(indexReader, Constants.Internals.FieldName);

            checker.IndexDictionary(dictionary);

            var suggestions = checker.SuggestSimilar(word, numberOfSuggestions);

            var metrics = suggestions.Select(s => new Suggestion
            {
                Word = s,
                Frequency = indexReader.DocFreq(new Term(Constants.Internals.FieldName, s)),
                Jaro = jaro.GetDistance(s, word),
                Leven = leven.GetDistance(s, word),
                NGram = ngram.GetDistance(s, word)
            })
            .OrderByDescending(metric => Priority(metric));

            var finalSuggestions = metrics.Select(s => new Suggestion
            {
                Word = s.Word,
                Frequency = s.Frequency,
                Priority = Priority(s)
            })
            .OrderByDescending(s => s.Priority);

            return finalSuggestions;
        }

        internal static float? Priority(Suggestion metric)
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
#endif