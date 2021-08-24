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
#if !NETCOREAPP
        private readonly BackgroundIndexRebuilder _backgroundIndexRebuilder;
        private readonly SpellCheckIndexCreator _spellCheckIndexCreator;
#endif

#if NETCOREAPP
        public IndexComponent(IExamineManager examineManager)
        {
            _examineManager = examineManager;
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

            if (_backgroundIndexRebuilder != null)
            {
                _backgroundIndexRebuilder.RebuildIndex();
            }
#endif
        }

        public void Terminate()
        {
            _examineManager.Dispose();
        }
    }
}