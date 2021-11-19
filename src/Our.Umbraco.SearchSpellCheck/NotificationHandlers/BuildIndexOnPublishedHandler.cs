#if NETCOREAPP
using Microsoft.Extensions.Options;
using Our.Umbraco.SearchSpellCheck.Indexing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Our.Umbraco.SearchSpellCheck.NotificationHandlers
{
    public class BuildIndexOnPublishedHandler : INotificationHandler<ContentPublishedNotification>
    {
        private readonly SpellCheckIndexRebuilder _spellCheckIndexRebuilder;
        private readonly SpellCheckOptions _options;

        public BuildIndexOnPublishedHandler(SpellCheckIndexRebuilder spellCheckIndexRebuilder, IOptions<SpellCheckOptions> options)
        {
            _spellCheckIndexRebuilder = spellCheckIndexRebuilder;
            _options = options.Value;
        }

        public void Handle(ContentPublishedNotification notification)
        {
            if (notification.PublishedEntities != null)
            {
                _spellCheckIndexRebuilder.RebuildIndex(_options.IndexName);
            }
        }
    }
}
#endif