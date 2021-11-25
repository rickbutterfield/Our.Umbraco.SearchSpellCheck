#if NETCOREAPP
using Lucene.Net.Index;
using Lucene.Net.Search.Spell;
using Lucene.Net.Store;
using Microsoft.Extensions.Options;
using Our.Umbraco.SearchSpellCheck.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Umbraco.Cms.Core.Hosting;

namespace Our.Umbraco.SearchSpellCheck.Services
{
    public class SuggestionService : ISuggestionService
    {
        private readonly SpellCheckOptions _options;
        private readonly IHostingEnvironment _hostingEnvironment;

        public SuggestionService(IOptions<SpellCheckOptions> options, IHostingEnvironment hostingEnvironment)
        {
            _options = options.Value;
            _hostingEnvironment = hostingEnvironment;
        }

        public string GetSuggestion(string searchTerm, int numberOfSuggestions = 10, float accuracy = 0.75f, string culture = null)
        {
            var words = searchTerm.Split(' ');
            var suggestions = new List<string>();
            foreach (string word in words)
            {
                var suggest = SuggestionData(word, numberOfSuggestions, accuracy, culture: culture);
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

        public IOrderedEnumerable<Suggestion> SuggestionData(string word, int numberOfSuggestions = 10, float searchAccuracy = 1f, string culture = null)
        {
            var luceneDirectory = GetFileSystemLuceneDirectory(_options.IndexName);
            var indexReader = DirectoryReader.Open(luceneDirectory);

            var jaro = new JaroWinklerDistance();
            var leven = new LuceneLevenshteinDistance();
            var ngram = new NGramDistance();

            var checker = new SpellChecker(new RAMDirectory(), jaro);
            string fieldName = Constants.Internals.FieldName;
            if (culture != null)
            {
                fieldName += $"_{culture}";
            }

            var fieldDictionary = new LuceneDictionary(indexReader, fieldName);
            var nodeNameDictionary = new LuceneDictionary(indexReader, "nodeName");

            var fieldConfig = new IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, null);
            var nodeNameConfig = new IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, null);

            checker.IndexDictionary(fieldDictionary, fieldConfig, true);
            checker.IndexDictionary(nodeNameDictionary, nodeNameConfig, true);

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

        public float? Priority(Suggestion metric)
        {
            return ((metric.Frequency / 100f) + metric.Jaro + metric.Leven + metric.NGram) / 4f;
        }

        public SimpleFSDirectory GetFileSystemLuceneDirectory(string indexName)
        {
            var dirInfo = new DirectoryInfo(
                Path.Combine(
                    _hostingEnvironment.MapPathContentRoot(global::Umbraco.Cms.Core.Constants.SystemDirectories.TempData),
                    "ExamineIndexes",
                    indexName));
            var luceneDir = new SimpleFSDirectory(dirInfo);
            return luceneDir;
        }
    }
}
#endif