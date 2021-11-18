#if NETCOREAPP
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Runtime;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Cms.Infrastructure.HostedServices;

namespace Our.Umbraco.SearchSpellCheck.RecurringTasks
{
    public class SpellCheckIndexRebuild : RecurringHostedServiceBase
    {
        private readonly IMainDom _mainDom;
        private readonly IRuntimeState _runtimeState;
        private readonly ILogger<SpellCheckIndexRebuild> _logger;
        private readonly ExamineIndexRebuilder _backgroundIndexRebuilder;
        private readonly SpellCheckOptions _options;

        public SpellCheckIndexRebuild(
            IRuntimeState runtimeState,
            IMainDom mainDom,
            ILogger<SpellCheckIndexRebuild> logger,
            ExamineIndexRebuilder backgroundIndexRebuilder,
            IOptions<SpellCheckOptions> options)
            : base(
                TimeSpan.FromMinutes(options.Value.AutoRebuildRepeat),
                TimeSpan.FromMinutes(options.Value.AutoRebuildDelay)
            )
        {
            _logger = logger;
            _mainDom = mainDom;
            _options = options.Value;
            _runtimeState = runtimeState;
            _backgroundIndexRebuilder = backgroundIndexRebuilder;
        }

        public override Task PerformExecuteAsync(object state)
        {
            if (_runtimeState.Level != RuntimeLevel.Run)
            {
                return Task.CompletedTask;
            }

            if (_mainDom.IsMainDom == false)
            {
                _logger.LogDebug("Does not run if not MainDom.");
                return Task.CompletedTask;
            }

            try
            {
                _backgroundIndexRebuilder.RebuildIndex(_options.IndexName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed.");
            }

            return Task.CompletedTask;
        }
    }
}
#endif