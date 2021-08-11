using Umbraco.Core.Logging;
using Umbraco.Core;
using Umbraco.Examine;
using Umbraco.Web.Scheduling;
using Our.Umbraco.SearchSpellCheck.BackgroundTasks;

namespace Our.Umbraco.SearchSpellCheck.Indexing
{
    public class BackgroundIndexRebuilder
    {
        private static readonly object RebuildLocker = new object();
        private readonly IndexRebuilder _indexRebuilder;
        private readonly IMainDom _mainDom;
        private readonly IProfilingLogger _logger;
        private static BackgroundTaskRunner<IBackgroundTask> _rebuildOnStartupRunner;

        public BackgroundIndexRebuilder(IMainDom mainDom, IProfilingLogger logger, IndexRebuilder indexRebuilder)
        {
            _mainDom = mainDom;
            _logger = logger;
            _indexRebuilder = indexRebuilder;
        }

        /// <summary>
        /// Called to rebuild empty index on startup
        /// </summary>
        /// <param name="indexRebuilder"></param>
        /// <param name="logger"></param>
        public void RebuildIndex()
        {
            lock (RebuildLocker)
            {
                if (_rebuildOnStartupRunner != null && _rebuildOnStartupRunner.IsRunning)
                {
                    _logger.Warn<BackgroundIndexRebuilder>("Call was made to RebuildIndexes but the task runner for rebuilding is already running");
                    return;
                }

                _logger.Info<BackgroundIndexRebuilder>("Starting initialize async background thread.");
                //do the rebuild on a managed background thread
                var task = new RebuildOnStartupTask(_mainDom, _indexRebuilder, _logger);

                _rebuildOnStartupRunner = new BackgroundTaskRunner<IBackgroundTask>(
                    "RebuildIndexesOnStartup",
                    _logger);

                _rebuildOnStartupRunner.TryAdd(task);
            }
        }
    }
}
