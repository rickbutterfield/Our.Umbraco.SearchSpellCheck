#if !NETCOREAPP
using Examine;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Examine;

namespace Our.Umbraco.SearchSpellCheck.Indexing
{
    public class SpellCheckIndexPopulator : IndexPopulator
    {
        private readonly SpellCheckValueSetBuilder _spellCheckValueSetBuilder;
        private readonly IContentService _contentService;
        private string _indexName;

        public SpellCheckIndexPopulator(SpellCheckValueSetBuilder spellCheckValueSetBuilder, IContentService contentService)
        {
            _spellCheckValueSetBuilder = spellCheckValueSetBuilder;
            _contentService = contentService;

            _indexName = ConfigurationManager.AppSettings[Constants.Configuration.IndexName];

            RegisterIndex(_indexName);
        }

        protected override void PopulateIndexes(IReadOnlyList<IIndex> indexes)
        {
            IContent[] content;
            long totalRecords = 0;
            int rootNode = -1;
            int pageIndex = 0;
            int pageSize = 10000;

            do
            {
                content = _contentService.GetPagedDescendants(rootNode, pageIndex, pageSize, out totalRecords).ToArray();

                if (content.Length > 0)
                {
                    var valueSets = _spellCheckValueSetBuilder.GetValueSets(content);

                    foreach (var index in indexes)
                    {
                        index.IndexItems(valueSets);
                    }
                }

                pageIndex++;
            }
            while (content.Length == pageSize);
        }
    }
}
#endif