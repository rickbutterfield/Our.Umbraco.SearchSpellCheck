#if NETCOREAPP
using Microsoft.Extensions.Options;
using Our.Umbraco.SearchSpellCheck.Indexing;
using System;
using System.Threading;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;
using Umbraco.Cms.Infrastructure.Examine;

namespace Our.Umbraco.SearchSpellCheck.NotificationHandlers
{
    internal class BuildIndexOnStartupHandler : INotificationHandler<UmbracoRequestBeginNotification>
    {
        private readonly ISyncBootStateAccessor _syncBootStateAccessor;
        private readonly ExamineIndexRebuilder _examineIndexRebuilder;
        private readonly IRuntimeState _runtimeState;
        private readonly SpellCheckOptions _spellCheckOptions;

        private static bool _isReady;
        private static bool _isReadSet;
        private static object _isReadyLock;

        public BuildIndexOnStartupHandler(
            ISyncBootStateAccessor syncBootStateAccessor,
            ExamineIndexRebuilder examineIndexRebuilder,
            IRuntimeState runtimeState,
            IOptions<SpellCheckOptions> spellCheckOptions)
        {
            _syncBootStateAccessor = syncBootStateAccessor;
            _examineIndexRebuilder = examineIndexRebuilder;
            _runtimeState = runtimeState;
            _spellCheckOptions = spellCheckOptions.Value;
        }

        /// <summary>
        /// On first http request schedule an index rebuild for any empty indexes (or all if it's a cold boot)
        /// </summary>
        /// <param name="notification"></param>
        public void Handle(UmbracoRequestBeginNotification notification)
        {
            if (_runtimeState.Level != RuntimeLevel.Run)
            {
                return;
            }

            LazyInitializer.EnsureInitialized(
                ref _isReady,
                ref _isReadSet,
                ref _isReadyLock,
                () =>
                {
                    SyncBootState bootState = _syncBootStateAccessor.GetSyncBootState();

                    _examineIndexRebuilder.RebuildIndex(
                        _spellCheckOptions.IndexName,
                        TimeSpan.FromMinutes(1));

                    return true;
                });
        }
    }
}
#endif