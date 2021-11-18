using Examine;
using System.Linq;
using System.Collections.Generic;
#if !NETCOREAPP
using Umbraco.Core.Models;
using Umbraco.Core.Services;
using Umbraco.Examine;
using System.Configuration;
#else
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Examine;
using Microsoft.Extensions.Options;
#endif

namespace Our.Umbraco.SearchSpellCheck.Indexing
{
#if !NETCOREAPP
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
#else
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
#endif
}