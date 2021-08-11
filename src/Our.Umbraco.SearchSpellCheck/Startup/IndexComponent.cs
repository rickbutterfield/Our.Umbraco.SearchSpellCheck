using Examine;
using Umbraco.Core.Composing;
using Our.Umbraco.SearchSpellCheck.Indexing;

namespace Our.Umbraco.SearchSpellCheck.Startup
{
    public class IndexComponent : IComponent
    {
        private readonly IExamineManager _examineManager;
        private readonly SpellCheckIndexCreator _spellCheckIndexCreator;
        private readonly BackgroundIndexRebuilder _backgroundIndexRebuilder;

        public IndexComponent(IExamineManager examineManager, SpellCheckIndexCreator spellCheckIndexCreator, BackgroundIndexRebuilder backgroundIndexRebuilder)
        {
            _examineManager = examineManager;
            _spellCheckIndexCreator = spellCheckIndexCreator;
            _backgroundIndexRebuilder = backgroundIndexRebuilder;
        }

        public void Initialize()
        {
            foreach (var index in _spellCheckIndexCreator.Create())
            {
                _examineManager.AddIndex(index);
            }

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
