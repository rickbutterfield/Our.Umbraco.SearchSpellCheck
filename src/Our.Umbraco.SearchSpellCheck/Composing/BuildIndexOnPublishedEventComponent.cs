#if !NETCOREAPP
using System;
using Umbraco.Core.Events;
using Umbraco.Core.Services;
using Umbraco.Core.Composing;
using Umbraco.Core.Services.Implement;
using Our.Umbraco.SearchSpellCheck.Indexing;
using System.Configuration;

namespace Our.Umbraco.SearchSpellCheck.Composing
{
    public class BuildIndexOnPublishedEventComponent : IComponent
    {
        private readonly BackgroundIndexRebuilder _backgroundIndexRebuilder;

        public BuildIndexOnPublishedEventComponent(BackgroundIndexRebuilder backgroundIndexRebuilder)
        {
            _backgroundIndexRebuilder = backgroundIndexRebuilder;
        }

        public void Initialize()
        {
            ContentService.Publishing += ContentService_Publishing;
        }

        private void ContentService_Publishing(IContentService sender, ContentPublishingEventArgs e)
        {
            if (e.PublishedEntities != null)
            {
                _backgroundIndexRebuilder.RebuildIndex();
            }
        }
        public void Terminate()
        {
            //unsubscribe during shutdown
            ContentService.Publishing -= ContentService_Publishing;
        }
    }
}
#endif