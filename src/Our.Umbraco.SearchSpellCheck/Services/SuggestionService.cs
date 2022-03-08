﻿#if NETCOREAPP
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Search.Spell;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Hosting;
#else
using Umbraco.Core.IO;
using System.Configuration;
using SpellChecker.Net.Search.Spell;
#endif
using System.IO;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Store;
using System.Collections.Generic;
using Our.Umbraco.SearchSpellCheck.Interfaces;


namespace Our.Umbraco.SearchSpellCheck.Services
{
    public class SuggestionService : ISuggestionService
    {
#if NETCOREAPP
        private readonly SpellCheckOptions _options;
        private readonly IHostingEnvironment _hostingEnvironment;

        public SuggestionService(IOptions<SpellCheckOptions> options, IHostingEnvironment hostingEnvironment)
        {
            _options = options.Value;
            _hostingEnvironment = hostingEnvironment;
        }
#endif

        public string GetSuggestion(string searchTerm, int numberOfSuggestions = 10, float suggestionAccuracy = 0.75f, string culture = null)
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
                        if (first.Priority > suggestionAccuracy)
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

        public IOrderedEnumerable<Suggestion> SuggestionData(string word, int numberOfSuggestions = 10, string culture = null)
        {
#if NETCOREAPP
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

            var analyser = new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48);
            var fieldConfig = new IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, analyser);
            var nodeNameConfig = new IndexWriterConfig(Lucene.Net.Util.LuceneVersion.LUCENE_48, analyser);

            checker.IndexDictionary(fieldDictionary, fieldConfig, true);
            checker.IndexDictionary(nodeNameDictionary, nodeNameConfig, true);
#else
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
#endif
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
#if NETCOREAPP
            var dirInfo = new DirectoryInfo(
                Path.Combine(
                    _hostingEnvironment.MapPathContentRoot(global::Umbraco.Cms.Core.Constants.SystemDirectories.TempData),
                    "ExamineIndexes",
                    indexName));
#else
            var dirInfo = new DirectoryInfo(Path.Combine(IOHelper.MapPath(SystemDirectories.TempData), "ExamineIndexes", indexName));
#endif
            var luceneDir = new SimpleFSDirectory(dirInfo);
            return luceneDir;
        }
    }
}