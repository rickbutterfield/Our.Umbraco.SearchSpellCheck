#if NETCOREAPP
using Umbraco.Cms.Core.Composing;
#else
using Umbraco.Core.Composing;
#endif
using Examine;
using Our.Umbraco.SearchSpellCheck.Indexing;
using Umbraco.Examine;
using Umbraco.Web.Scheduling;
using Umbraco.Core.Logging;
using Our.Umbraco.SearchSpellCheck.RecurringTasks;
using System.Configuration;

namespace Our.Umbraco.SearchSpellCheck.Startup
{
    public class IndexComponent : IComponent
    {
        private readonly IExamineManager _examineManager;
#if !NETCOREAPP
        private readonly IProfilingLogger _logger;
        private readonly IndexRebuilder _indexRebuilder;
        private readonly SpellCheckIndexCreator _spellCheckIndexCreator;
        private readonly BackgroundIndexRebuilder _backgroundIndexRebuilder;
        private readonly BackgroundTaskRunner<IBackgroundTask> _indexRebuildRunner;
#endif

#if NETCOREAPP
        public IndexComponent(IExamineManager examineManager)
        {
            _examineManager = examineManager;
        }
#else
        public IndexComponent(IExamineManager examineManager, SpellCheckIndexCreator spellCheckIndexCreator, BackgroundIndexRebuilder backgroundIndexRebuilder, IndexRebuilder indexRebuilder, IProfilingLogger logger)
        {
            _logger = logger;
            _examineManager = examineManager;
            _spellCheckIndexCreator = spellCheckIndexCreator;
            _backgroundIndexRebuilder = backgroundIndexRebuilder;
            _indexRebuilder = indexRebuilder;
            _indexRebuildRunner = new BackgroundTaskRunner<IBackgroundTask>("SpellCheckIndexRebuild", _logger);
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

            if (_indexRebuildRunner != null)
            {
                bool autoRebuildIndex = false;
                bool.TryParse(ConfigurationManager.AppSettings[Constants.Configuration.AutoRebuildIndex], out autoRebuildIndex);

                if (autoRebuildIndex)
                {
                    int delayBeforeWeStart = 300000;
                    int howOftenWeRepeat = 3600000;

                    int.TryParse(ConfigurationManager.AppSettings[Constants.Configuration.AutoRebuildDelay], out delayBeforeWeStart);
                    int.TryParse(ConfigurationManager.AppSettings[Constants.Configuration.AutoRebuildRepeat], out howOftenWeRepeat);

                    var task = new IndexRebuild(_indexRebuildRunner, delayBeforeWeStart, howOftenWeRepeat, _logger, _indexRebuilder);

                    //As soon as we add our task to the runner it will start to run (after its delay period)
                    _indexRebuildRunner.TryAdd(task);
                }
            }
#endif
        }

        public void Terminate()
        {
            _examineManager.Dispose();
        }
    }
}