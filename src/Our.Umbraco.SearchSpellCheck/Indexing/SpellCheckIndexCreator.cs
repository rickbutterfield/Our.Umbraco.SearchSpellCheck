#if !NETCOREAPP
using Examine;
using Lucene.Net.Util;
using Umbraco.Examine;
using System.Collections.Generic;
using Lucene.Net.Analysis.Standard;
using Examine.LuceneEngine.Providers;
using System.Configuration;

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
                new StandardAnalyzer(Version.LUCENE_30)
            );

            return new[] { index };
        }
    }
}
#endif