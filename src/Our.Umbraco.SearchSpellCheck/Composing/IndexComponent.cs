#if !NETCOREAPP
using Examine;
using Umbraco.Core.Composing;
using Our.Umbraco.SearchSpellCheck.Indexing;
using Umbraco.Examine;
using Umbraco.Web.Scheduling;
using Umbraco.Core.Logging;
using Our.Umbraco.SearchSpellCheck.RecurringTasks;
using System.Configuration;
using System;

namespace Our.Umbraco.SearchSpellCheck.Composing
{
    public class IndexComponent : IComponent
    {
        private readonly IProfilingLogger _logger;
        private readonly IndexRebuilder _indexRebuilder;
        private readonly SpellCheckIndexCreator _spellCheckIndexCreator;
        private readonly BackgroundIndexRebuilder _backgroundIndexRebuilder;
        private readonly BackgroundTaskRunner<IBackgroundTask> _indexRebuildRunner;
        private readonly IExamineManager _examineManager;

        public IndexComponent(IExamineManager examineManager, SpellCheckIndexCreator spellCheckIndexCreator, BackgroundIndexRebuilder backgroundIndexRebuilder, IndexRebuilder indexRebuilder, IProfilingLogger logger)
        {
            _logger = logger;
            _examineManager = examineManager;
            _spellCheckIndexCreator = spellCheckIndexCreator;
            _backgroundIndexRebuilder = backgroundIndexRebuilder;
            _indexRebuilder = indexRebuilder;
            _indexRebuildRunner = new BackgroundTaskRunner<IBackgroundTask>("SpellCheckIndexRebuild", _logger);
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

            if (_indexRebuildRunner != null)
            {
                bool autoRebuildIndex = true;
                var autoRebuildConfig = ConfigurationManager.AppSettings[Constants.Configuration.AutoRebuildIndex];
                if (autoRebuildConfig != null)
                {
                    bool.TryParse(autoRebuildConfig, out autoRebuildIndex);
                }

                if (autoRebuildIndex)
                {
                    int delayBeforeWeStart = 5;
                    int howOftenWeRepeat = 30;

                    var autoRebuildDelay = ConfigurationManager.AppSettings[Constants.Configuration.AutoRebuildDelay];
                    if (autoRebuildDelay != null)
                    {
                        int.TryParse(autoRebuildDelay, out delayBeforeWeStart);
                    }

                    var autoRebuildRepeat = ConfigurationManager.AppSettings[Constants.Configuration.AutoRebuildRepeat];
                    if (autoRebuildRepeat != null)
                    {
                        int.TryParse(autoRebuildRepeat, out howOftenWeRepeat);
                    }

                    TimeSpan delay = TimeSpan.FromMinutes(delayBeforeWeStart);
                    TimeSpan repeat = TimeSpan.FromMinutes(howOftenWeRepeat);

                    var task = new IndexRebuild(_indexRebuildRunner, ((int)delay.TotalMilliseconds), ((int)repeat.TotalMilliseconds), _logger, _indexRebuilder);

                    //As soon as we add our task to the runner it will start to run (after its delay period)
                    _indexRebuildRunner.TryAdd(task);
                }
            }
        }

        public void Terminate()
        {
            _examineManager.Dispose();
        }
    }
}
#endif