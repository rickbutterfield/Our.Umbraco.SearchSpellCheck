#if NETCOREAPP
using Umbraco.Cms.Core.Composing;
#else
using Umbraco.Core.Composing;
#endif
using Examine;
using Our.Umbraco.SearchSpellCheck.Indexing;

namespace Our.Umbraco.SearchSpellCheck.Startup
{
    public class IndexComponent : IComponent
    {
        private readonly IExamineManager _examineManager;
        private readonly BackgroundIndexRebuilder _backgroundIndexRebuilder;
#if !NETCOREAPP
        private readonly SpellCheckIndexCreator _spellCheckIndexCreator;
#endif

#if NETCOREAPP
        public IndexComponent(IExamineManager examineManager, BackgroundIndexRebuilder backgroundIndexRebuilder)
        {
            _examineManager = examineManager;
            _backgroundIndexRebuilder = backgroundIndexRebuilder;
        }
#else
        public IndexComponent(IExamineManager examineManager, SpellCheckIndexCreator spellCheckIndexCreator, BackgroundIndexRebuilder backgroundIndexRebuilder)
        {
            _examineManager = examineManager;
            _spellCheckIndexCreator = spellCheckIndexCreator;
            _backgroundIndexRebuilder = backgroundIndexRebuilder;
        }
#endif

        public void Initialize()
        {
#if !NETCOREAPP
            foreach (var index in _spellCheckIndexCreator.Create())
            {
                _examineManager.AddIndex(index);
            }
#endif

            if (_backgroundIndexRebuilder != null)
            {
                _backgroundIndexRebuilder.RebuildIndex();
            }
        }

        public void Terminate()
        {
            _examineManager.Dispose();
        }
    }
}