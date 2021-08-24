#if NETCOREAPP
using Umbraco.Cms.Core.Runtime;
using Microsoft.Extensions.Logging;
#else
using Umbraco.Core;
using Umbraco.Examine;
using Umbraco.Core.Logging;
using Umbraco.Web.Scheduling;
using Our.Umbraco.SearchSpellCheck.BackgroundTasks;
#endif

#if !NETCOREAPP
namespace Our.Umbraco.SearchSpellCheck.Indexing
{
    public class BackgroundIndexRebuilder
    {
        private static readonly object RebuildLocker = new object();
        private readonly IndexRebuilder _indexRebuilder;
        private readonly IMainDom _mainDom;
        private readonly ILogger _logger;
        private static BackgroundTaskRunner<IBackgroundTask> _rebuildOnStartupRunner;

        public BackgroundIndexRebuilder(IMainDom mainDom, ILogger logger, IndexRebuilder indexRebuilder)
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
#if NETCOREAPP
                    _logger.LogWarning("Call was made to RebuildIndexes but the task runner for rebuilding is already running");
#else
                    _logger.Warn<BackgroundIndexRebuilder>("Call was made to RebuildIndexes but the task runner for rebuilding is already running");
#endif
                    return;
                }

#if NETCOREAPP
                _logger.LogInformation("Starting initialize async background thread.");
#else
                _logger.Info<BackgroundIndexRebuilder>("Starting initialize async background thread.");
#endif

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
#endif