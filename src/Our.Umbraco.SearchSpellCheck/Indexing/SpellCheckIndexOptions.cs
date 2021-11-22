#if NETCOREAPP
using Examine.Lucene;
using Examine.Lucene.Analyzers;
using Lucene.Net.Index;
using Microsoft.Extensions.Options;
using System;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Infrastructure.Examine;

namespace Our.Umbraco.SearchSpellCheck
{
    public class SpellCheckIndexOptions : IConfigureNamedOptions<LuceneDirectoryIndexOptions>
    {
        private readonly IUmbracoIndexConfig _umbracoIndexConfig;
        private readonly IOptions<IndexCreatorSettings> _settings;
        private readonly IOptions<SpellCheckOptions> _options;

        public SpellCheckIndexOptions(
            IUmbracoIndexConfig umbracoIndexConfig,
            IOptions<IndexCreatorSettings> settings,
            IOptions<SpellCheckOptions> options)
        {
            _umbracoIndexConfig = umbracoIndexConfig;
            _settings = settings;
            _options = options;
        }

        public void Configure(string name, LuceneDirectoryIndexOptions options)
        {
            if (name == _options.Value.IndexName)
            {
                options.Analyzer = new CultureInvariantWhitespaceAnalyzer();
                options.Validator = _umbracoIndexConfig.GetPublishedContentValueSetValidator();
                options.FieldDefinitions = new SpellCheckIndexFieldDefinitionCollection();
                options.UnlockIndex = true;

                if (_settings.Value.LuceneDirectoryFactory == LuceneDirectoryFactory.SyncedTempFileSystemDirectoryFactory)
                {
                    // if this directory factory is enabled then a snapshot deletion policy is required
                    options.IndexDeletionPolicy = new SnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy());
                }
            }
        }

        public void Configure(LuceneDirectoryIndexOptions options)
            => throw new NotImplementedException("This is never called and is just part of the interface");
    }
}
#endif