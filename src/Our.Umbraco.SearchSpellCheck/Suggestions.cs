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
                if (suggest.First() != null)
                {
                    if (suggest.First().Priority > accuracy)
                    {
                        suggestions.Add(suggest.First().Word);
                    }
                    else
                    {
                        suggestions.Add(word);
                    }
                }
            }

            return string.Join(" ", suggestions);
        }

        internal static IOrderedEnumerable<Suggestion> SuggestionData(string word, int numberOfSuggestions = 10, float searchAccuracy = 1f)
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
            checker.IndexDictionary(new LuceneDictionary(indexReader, Constants.Internals.FieldName), null, true);
            var suggestions = checker.SuggestSimilar(word, numberOfSuggestions, searchAccuracy);
#else
            var checker = new SpellChecker.Net.Search.Spell.SpellChecker(new RAMDirectory(), jaro);
            checker.IndexDictionary(new LuceneDictionary(indexReader, Constants.Internals.FieldName));
            checker.SetAccuracy(searchAccuracy);
            var suggestions = checker.SuggestSimilar(word, numberOfSuggestions);
#endif

            var metrics = suggestions.Select(s => new Suggestion
            {
                Word = s,
                Frequency = indexReader.DocFreq(new Term(Constants.Internals.FieldName, s)),
                Jaro = jaro.GetDistance(word, s),
                Leven = leven.GetDistance(word, s),
                NGram = ngram.GetDistance(word, s)
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