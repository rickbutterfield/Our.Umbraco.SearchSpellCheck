#if NETCOREAPP
using Our.Umbraco.SearchSpellCheck.Indexing;
using System;
using System.Threading;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Sync;

namespace Our.Umbraco.SearchSpellCheck.NotificationHandlers
{
    internal class BuildIndexOnStartupHandler : INotificationHandler<UmbracoRequestBeginNotification>
    {
        private readonly ISyncBootStateAccessor _syncBootStateAccessor;
        private readonly SpellCheckIndexRebuilder _backgroundIndexRebuilder;
        private readonly IRuntimeState _runtimeState;

        private static bool _isReady;
        private static bool _isReadSet;
        private static object _isReadyLock;

        public BuildIndexOnStartupHandler(
            ISyncBootStateAccessor syncBootStateAccessor,
            SpellCheckIndexRebuilder backgroundIndexRebuilder,
            IRuntimeState runtimeState)
        {
            _syncBootStateAccessor = syncBootStateAccessor;
            _backgroundIndexRebuilder = backgroundIndexRebuilder;
            _runtimeState = runtimeState;
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

                    _backgroundIndexRebuilder.RebuildIndexes(
                        // if it's not a cold boot, only rebuild empty ones
                        bootState != SyncBootState.ColdBoot,
                        TimeSpan.FromMinutes(1));

                    return true;
                });
        }
    }
}
#endif