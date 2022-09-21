using Examine;
using System.Linq;
using System.Collections.Generic;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Examine;
using Microsoft.Extensions.Options;

namespace Our.Umbraco.SearchSpellCheck.Indexing
{
    public class SpellCheckIndexPopulator : IndexPopulator<IUmbracoContentIndex>
    {
        private readonly SpellCheckValueSetBuilder _spellCheckValueSetBuilder;
        private readonly IContentService _contentService;
        private readonly SpellCheckOptions _options;
        private string _indexName;

        public SpellCheckIndexPopulator(SpellCheckValueSetBuilder spellCheckValueSetBuilder, IContentService contentService, IOptions<SpellCheckOptions> options)
        {
            _spellCheckValueSetBuilder = spellCheckValueSetBuilder;
            _contentService = contentService;
            _options = options.Value;

            _indexName = _options.IndexName;
        }

        protected override void PopulateIndexes(IReadOnlyList<IIndex> indexes)
        {
            IContent[] content;
            long totalRecords = 0;
            int rootNode = -1;
            int pageIndex = 0;
            int pageSize = 500;

            do
            {
                content = _contentService.GetPagedDescendants(rootNode, pageIndex, pageSize, out totalRecords).ToArray();

                if (content.Length > 0)
                {
                    var valueSets = _spellCheckValueSetBuilder.GetValueSets(content).ToList();

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