#if !NETCOREAPP
using Examine;
using Umbraco.Examine;
using Examine.LuceneEngine;
using System.Configuration;
using System.Collections.Generic;
using Examine.LuceneEngine.Providers;

namespace Our.Umbraco.SearchSpellCheck.Indexing
{
    public class SpellCheckIndexCreator : LuceneIndexCreator
    {
        private string _indexName;

        public SpellCheckIndexCreator()
        {
            _indexName = ConfigurationManager.AppSettings[Constants.Configuration.IndexName];
        }

        public override IEnumerable<IIndex> Create()
        {
            LuceneIndex index = new LuceneIndex(_indexName,
                CreateFileSystemLuceneDirectory(_indexName),
                new FieldDefinitionCollection(
                    new FieldDefinition(Constants.Internals.FieldName, FieldDefinitionTypes.FullText)
                ),
                new CultureInvariantWhitespaceAnalyzer()
            );

            return new[] { index };
        }
    }
}
#endif