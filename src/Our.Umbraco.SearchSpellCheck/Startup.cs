#if NETCOREAPP
using Examine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Our.Umbraco.SearchSpellCheck.Indexing;
using Our.Umbraco.SearchSpellCheck.Interfaces;
using Our.Umbraco.SearchSpellCheck.NotificationHandlers;
using Our.Umbraco.SearchSpellCheck.RecurringTasks;
using Our.Umbraco.SearchSpellCheck.Services;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Extensions;

namespace Our.Umbraco.SearchSpellCheck
{
    public class Startup : IComposer
    {
        public void Compose(IUmbracoBuilder builder)
        {
            // Configuration
            builder.Services.Configure<SpellCheckOptions>(builder.Config.GetSection(Constants.Configuration.ConfigurationSection));
            var options = builder.Config.GetSection(Constants.Configuration.ConfigurationSection).Get<SpellCheckOptions>();

            // Indexes
            builder.Services
                .AddExamineLuceneIndex<SpellCheckIndex, ConfigurationEnabledDirectoryFactory>(options.IndexName)
                .ConfigureOptions<SpellCheckIndexOptions>();
            builder.Services.AddSingleton<SpellCheckValueSetBuilder>();
            builder.Services.AddSingleton<IIndexPopulator, SpellCheckIndexPopulator>();

            if (options.AutoRebuildIndex)
            {
                builder.Services.AddHostedService<SpellCheckIndexRebuild>();
            }

            // Notification handlers
            if (options.BuildOnStartup)
            {
                builder.AddNotificationHandler<UmbracoRequestBeginNotification, BuildIndexOnStartupHandler>();
            }

            if (options.RebuildOnPublish)
            {
                builder.AddNotificationHandler<ContentPublishedNotification, BuildIndexOnPublishedHandler>();
            }

            // Services
            builder.Services.AddSingleton<ISuggestionService, SuggestionService>();
        }
    }
}
#endif