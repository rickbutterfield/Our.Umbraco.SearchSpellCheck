using System;
using Umbraco.Core;
using Umbraco.Examine;
using System.Threading;
using Umbraco.Core.Logging;
using System.Threading.Tasks;
using Umbraco.Web.Scheduling;
using System.Configuration;

namespace Our.Umbraco.SearchSpellCheck.BackgroundTasks
{
    /// <summary>
    /// Background task used to rebuild empty indexes on startup
    /// </summary>
    internal class RebuildOnStartupTask : IBackgroundTask
    {
        private readonly IMainDom _mainDom;
        private readonly IndexRebuilder _indexRebuilder;
        private readonly ILogger _logger;
        private string _indexName;

        public RebuildOnStartupTask(IMainDom mainDom,
                IndexRebuilder indexRebuilder, ILogger logger)
        {
            _mainDom = mainDom;
            _indexRebuilder = indexRebuilder ?? throw new ArgumentNullException(nameof(indexRebuilder));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _indexName = ConfigurationManager.AppSettings[Constants.Configuration.IndexName];
        }

        public bool IsAsync => false;

        public void Dispose()
        {
        }

        public void Run()
        {
            try
            {
                // rebuilds indexes
                RebuildIndexes();
            }
            catch (Exception ex)
            {
                _logger.Error<RebuildOnStartupTask>(ex, "Failed to rebuild empty indexes.");
            }
        }

        public Task RunAsync(CancellationToken token)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Used to rebuild indexes on startup or cold boot
        /// </summary>
        private void RebuildIndexes()
        {
            //do not attempt to do this if this has been disabled since we are not the main dom.
            //this can be called during a cold boot
            if (!_mainDom.IsMainDom) return;

            _indexRebuilder.RebuildIndex(_indexName);
        }
    }
}
