#if NETCOREAPP
using Examine;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Cms.Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
#else
using Umbraco.Core;
using Umbraco.Core.Composing;
#endif
using Our.Umbraco.SearchSpellCheck.Indexing;

namespace Our.Umbraco.SearchSpellCheck.Startup
{
#if !NETCOREAPP
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
#endif
    public class IndexComposer : IUserComposer
    {
#if NETCOREAPP
        public void Compose(IUmbracoBuilder builder)
        {
            builder.Services.AddExamineLuceneIndex<SpellCheckIndex, ConfigurationEnabledDirectoryFactory>("SpellCheckIndex");
        }
#else
        public void Compose(Composition composition)
        {
            composition.RegisterUnique<BackgroundIndexRebuilder>();

            composition.RegisterUnique<SpellCheckValueSetBuilder>();
            composition.Register<SpellCheckIndexPopulator>(Lifetime.Singleton);
            composition.RegisterUnique<SpellCheckIndexCreator>();

            composition.Components().Append<IndexComponent>();
        }
#endif
    }
}
