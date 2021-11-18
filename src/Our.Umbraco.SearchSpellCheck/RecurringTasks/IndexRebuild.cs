#if !NETCOREAPP
using Examine;
using Umbraco.Examine;
using Umbraco.Core.Logging;
using Umbraco.Web.Scheduling;
using System.Configuration;

namespace Our.Umbraco.SearchSpellCheck.RecurringTasks
{
    public class IndexRebuild : RecurringTaskBase
    {
        private readonly IProfilingLogger _logger;
        private readonly IndexRebuilder _indexRebuilder;

        public IndexRebuild(IBackgroundTaskRunner<RecurringTaskBase> runner, int delayBeforeWeStart, int howOftenWeRepeat, IProfilingLogger logger, IndexRebuilder indexRebuilder)
            : base(runner, delayBeforeWeStart, howOftenWeRepeat)
        {
            _logger = logger;
            _indexRebuilder = indexRebuilder;
        }

        public override bool IsAsync => false;

        public override bool PerformRun()
        {
            string indexName = ConfigurationManager.AppSettings[Constants.Configuration.IndexName] ?? Constants.Configuration.DefaultIndexName;

            if (ExamineManager.Instance.TryGetIndex(indexName, out IIndex index))
            {
                if (_indexRebuilder.CanRebuild(index))
                {
                    _indexRebuilder.RebuildIndex(indexName);
                    return true;
                }
            }

            return false;
        }
    }
}
#endif