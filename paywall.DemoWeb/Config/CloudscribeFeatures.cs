using cloudscribe.EmailQueue.HangfireIntegration;
using cloudscribe.EmailQueue.Models;
using cloudscribe.Membership.HangfireIntegration;
using cloudscribe.Membership.Models;
using Hangfire;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CloudscribeFeatures
    {
        public static IServiceCollection SetupDataStorage(
            this IServiceCollection services,
            IConfiguration config
            )
        {
            var connectionString = config.GetConnectionString("EntityFrameworkConnection");

            services.AddCloudscribeCoreEFStorageMSSQL(connectionString);
            services.AddCloudscribeLoggingEFStorageMSSQL(connectionString);
            
            services.AddCloudscribeSimpleContentEFStorageMSSQL(connectionString);

            services.AddEmailTemplateStorageMSSQL(connectionString);
            services.AddEmailQueueStorageMSSQL(connectionString);
            services.AddMembershipSubscriptionStorageMSSQL(connectionString);
            services.AddHangfire(hfConfig => hfConfig.UseSqlServerStorage(connectionString));

            return services;
        }

        public static IServiceCollection SetupCloudscribeFeatures(
            this IServiceCollection services,
            IConfiguration config
            )
        {

            services.AddCloudscribeLogging();


            services.AddScoped<cloudscribe.Web.Navigation.INavigationNodePermissionResolver, cloudscribe.Web.Navigation.NavigationNodePermissionResolver>();
            services.AddScoped<cloudscribe.Web.Navigation.INavigationNodePermissionResolver, cloudscribe.SimpleContent.Web.Services.PagesNavigationNodePermissionResolver>();
            services.AddCloudscribeCoreMvc(config);
            services.AddCloudscribeCoreIntegrationForSimpleContent(config);
            services.AddSimpleContentMvc(config);
            services.AddMetaWeblogForSimpleContent(config.GetSection("MetaWeblogApiOptions"));
            services.AddSimpleContentRssSyndiction();

            services.AddScoped<IRoleRemovalTask, HangfireRoleRemovalTask>();
            services.AddScoped<ISendRemindersTask, HangfireSendRemindersTask>();
            services.AddMembershipSubscriptionMvcComponents(config);

            // for testing the message formatting and token replacement
            //services.AddScoped<IEmailQueueItemSender, cloudscribe.EmailQueue.Services.LogOnlyMessageSender>();
            services.AddScoped<IEmailQueueProcessor, HangFireEmailQueueProcessor>();
            services.AddEmailQueueWithCloudscribeIntegration(config);

            services.AddEmailRazorTemplating(config);

            return services;
        }

    }
}
