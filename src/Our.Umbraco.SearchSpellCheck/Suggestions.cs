#if !NETCOREAPP
using Umbraco.Core.IO;
using System.Configuration;
using SpellChecker.Net.Search.Spell;
using System.IO;
using System.Linq;
using Lucene.Net.Store;
using Lucene.Net.Index;
using System.Collections.Generic;
using System;

namespace Our.Umbraco.SearchSpellCheck
{
    public static class Suggestions
    {
        [Obsolete("Use the ISuggestionService injected into your constructor")]
        public static string GetSuggestion(string searchTerm, int numberOfSuggestions = 10, float suggestionAccurary = 0.75f, string culture = null)
        {
            var words = searchTerm.Split(' ');
            var suggestions = new List<string>();
            foreach (string word in words)
            {
                var suggest = SuggestionData(word, numberOfSuggestions, culture);
                if (suggest != null)
                {
                    var first = suggest.FirstOrDefault();
                    if (first != null)
                    {
                        if (first.Priority > suggestionAccurary)
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

        internal static IOrderedEnumerable<Suggestion> SuggestionData(string word, int numberOfSuggestions = 10, string culture = null)
        {
            string indexName = ConfigurationManager.AppSettings[Constants.Configuration.IndexName] ?? Constants.Configuration.DefaultIndexName;

            var indexReader = IndexReader.Open(GetFileSystemLuceneDirectory(indexName), true);

            var jaro = new JaroWinklerDistance();
            var leven = new LevenshteinDistance();
            var ngram = new NGramDistance();

            var checker = new SpellChecker.Net.Search.Spell.SpellChecker(new RAMDirectory(), jaro);
            string fieldName = Constants.Internals.FieldName;
            if (culture != null)
            {
              fieldName += $"_{culture}";
            }

            var fieldDictionary = new LuceneDictionary(indexReader, fieldName);
            var nodeNameDictionary = new LuceneDictionary(indexReader, "nodeName");

            checker.IndexDictionary(fieldDictionary);
            checker.IndexDictionary(nodeNameDictionary);

            var suggestions = checker.SuggestSimilar(word, numberOfSuggestions);

            var metrics = suggestions.Select(s => new Suggestion
            {
                Word = s,
                Frequency = indexReader.DocFreq(new Term(fieldName, s)),
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