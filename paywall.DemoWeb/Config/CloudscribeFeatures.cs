using cloudscribe.Membership.HangfireIntegration;
using cloudscribe.Membership.Models;
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
            var dbPlatform = config["DataSettings:DbPlatform"];
            switch (dbPlatform)
            {
                case "pgsql":
                    var pgSqlConnectionString = config["DataSettings:PostgreSqlConnectionString"];
                    
                    services.AddCloudscribeLoggingPostgreSqlStorage(pgSqlConnectionString);
                    services.AddCloudscribeCorePostgreSqlStorage(pgSqlConnectionString);
                    services.AddCloudscribeSimpleContentPostgreSqlStorage(pgSqlConnectionString);
                    services.AddMembershipSubscriptionPostgreSqlStorage(pgSqlConnectionString);
                    services.AddStripeIntegrationPostgreSqlStorage(pgSqlConnectionString);
                    services.AddEmailTemplatePostgreSqlStorage(pgSqlConnectionString);
                    services.AddEmailQueuePostgreSqlStorage(pgSqlConnectionString);
                    services.AddDynamicPolicyPostgreSqlStorage(pgSqlConnectionString);

                    break;

                case "mysql":
                    var mySqlConnectionString = config["DataSettings:MySqlConnectionString"];
                    

                    services.AddCloudscribeLoggingEFStorageMySQL(mySqlConnectionString);
                    services.AddCloudscribeCoreEFStorageMySql(mySqlConnectionString);
                    services.AddCloudscribeSimpleContentEFStorageMySQL(mySqlConnectionString);
                    services.AddMembershipSubscriptionStorageMySql(mySqlConnectionString);
                    services.AddStripeIntegrationStorageMySql(mySqlConnectionString);
                    services.AddEmailTemplateStorageMySql(mySqlConnectionString);
                    services.AddEmailQueueStorageMySql(mySqlConnectionString);
                    services.AddDynamicPolicyEFStorageMySql(mySqlConnectionString);

                    break;

                case "mssql":
                default:

                    var msSqlConnectionString = config["DataSettings:MsSqlConnectionString"];
                   

                    services.AddCloudscribeLoggingEFStorageMSSQL(msSqlConnectionString);
                    services.AddCloudscribeCoreEFStorageMSSQL(msSqlConnectionString);
                    services.AddCloudscribeSimpleContentEFStorageMSSQL(msSqlConnectionString);
                    services.AddMembershipSubscriptionStorageMSSQL(msSqlConnectionString);
                    services.AddStripeIntegrationStorageMSSQL(msSqlConnectionString);
                    services.AddEmailTemplateStorageMSSQL(msSqlConnectionString);
                    services.AddEmailQueueStorageMSSQL(msSqlConnectionString);
                    services.AddDynamicPolicyEFStorageMSSQL(msSqlConnectionString);

                    

                    break;

            }
            
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

            services.AddMembershipSStripeIntegration(config);
            services.AddCloudscribeCoreStripeIntegration();
            services.AddStripeIntegrationMvc(config);

            services.AddScoped<IRoleRemovalTask, HangfireRoleRemovalTask>();
            services.AddScoped<ISendRemindersTask, HangfireSendRemindersTask>();
            services.AddMembershipSubscriptionMvcComponents(config);
            services.AddMembershipBackgroundTasks(config);

            // for testing the message formatting and token replacement
            //services.AddScoped<IEmailQueueItemSender, cloudscribe.EmailQueue.Services.LogOnlyMessageSender>();
            services.AddEmailQueueBackgroundTask(config);
            services.AddEmailQueueWithCloudscribeIntegration(config);

            services.AddEmailRazorTemplating(config);

            services.AddCloudscribeDynamicPolicyIntegration(config);
            services.AddDynamicAuthorizationMvc(config);

            return services;
        }

    }
}
