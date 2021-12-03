#if !NETCOREAPP
using Umbraco.Core;
using Umbraco.Core.Composing;
using Our.Umbraco.SearchSpellCheck.Indexing;
using Our.Umbraco.SearchSpellCheck.Interfaces;
using Our.Umbraco.SearchSpellCheck.Services;

namespace Our.Umbraco.SearchSpellCheck.Composing
{
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class IndexComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.RegisterUnique<BackgroundIndexRebuilder>();

            composition.RegisterUnique<SpellCheckValueSetBuilderV8>();
            composition.Register<SpellCheckIndexPopulator>(Lifetime.Singleton);
            composition.RegisterUnique<SpellCheckIndexCreator>();

            composition.Components().Append<IndexComponent>();

            composition.Register<ISuggestionService, SuggestionService>();
        }
    }
}
#endif