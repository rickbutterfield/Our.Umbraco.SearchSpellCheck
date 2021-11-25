#if NETCOREAPP
using Microsoft.Extensions.Options;
using Our.Umbraco.SearchSpellCheck.Indexing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Infrastructure.Examine;

namespace Our.Umbraco.SearchSpellCheck.NotificationHandlers
{
    public class BuildIndexOnPublishedHandler : INotificationHandler<ContentPublishedNotification>
    {
        private readonly ExamineIndexRebuilder _examineIndexRebuilder;
        private readonly SpellCheckOptions _options;

        public BuildIndexOnPublishedHandler(ExamineIndexRebuilder examineIndexRebuilder, IOptions<SpellCheckOptions> options)
        {
            _examineIndexRebuilder = examineIndexRebuilder;
            _options = options.Value;
        }

        public void Handle(ContentPublishedNotification notification)
        {
            if (notification.PublishedEntities != null)
            {
                _examineIndexRebuilder.RebuildIndex(_options.IndexName);
            }
        }
    }
}
#endif