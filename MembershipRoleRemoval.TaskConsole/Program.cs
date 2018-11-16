using cloudscribe.Membership.Models;
using cloudscribe.Membership.Services;
using cloudscribe.Membership.StripeIntegration;
using cloudscribe.StripeIntegration.Models;
using cloudscribe.StripeIntegration.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;

namespace MembershipRoleRemoval.TaskConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "cloudscribe Email Queue/Email List Processor task";

            var hostBuilder = GetHostBuilder(args);
            var host = hostBuilder.Build();

            using (var scope = host.Services.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                try
                {
                    MembershipDatabase.InitializeDatabaseAsync(scopedServices).Wait();
                }
                catch (Exception ex)
                {
                    var logger = scopedServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while migrating the database.");
                }
            }

            var job = host.Services.GetRequiredService<IRoleRemovalTask>();
            job.RemoveExpiredMembersFromGrantedRoles().Wait();

        }

        private static IConfiguration config;

        public static IHostBuilder GetHostBuilder(string[] args)
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile("appsettings.json", optional: false);
#if DEBUG
            builder.AddJsonFile("appsettings.Development.json", optional: true);
#endif
            builder.AddJsonFile("appsettings.Production.json", optional: true);

            config = builder.Build();

            var cultureCode = config["AppSettings:ThreadCulture"];
            if (string.IsNullOrWhiteSpace(cultureCode))
            {
                cultureCode = "en-US";
            }

            var culture = new CultureInfo(cultureCode);
            CultureInfo.CurrentCulture = culture;

            var hostBuilder = new HostBuilder()
                .ConfigureAppConfiguration(configBuilder =>
                {
                    configBuilder.AddJsonFile("appsettings.json", optional: false);
#if DEBUG
                    configBuilder.AddJsonFile("appsettings.Development.json", optional: true);
#endif
                    configBuilder.AddJsonFile("appsettings.Production.json", optional: true);

                })
               .ConfigureServices((hostContext, services) =>
               {
                   services.AddLogging(configure =>
                   {
                       configure.AddConsole();

                       var logToEventLog = config.GetValue<bool>("AppSettings:LogToWindowsEventLog");
                       var logLevel = config.GetValue<string>("AppSettings:MinimumLogLevel");

                       if (logToEventLog)
                       {
                           configure.AddEventLog();
                       }

                       switch (logLevel)
                       {
                           case "Debug":
                               configure.SetMinimumLevel(LogLevel.Debug);
                               break;

                           case "Information":
                               configure.SetMinimumLevel(LogLevel.Information);
                               break;

                           case "Trace":
                               configure.SetMinimumLevel(LogLevel.Trace);
                               break;

                           default:
                               configure.SetMinimumLevel(LogLevel.Warning);
                               break;
                       }
                   });

                   var maxConnectionRetries = 4;
                   var useSingletonLifetime = true;
                   var dbPlatform = config["AppSettings:DbPlatform"];

                   switch (dbPlatform)
                   {
                       case "pgsql":
                           var pgSqlConnectionString = config["DataSettings:PostgreSqlConnectionString"];
                           services.AddCloudscribeCorePostgreSqlStorage(pgSqlConnectionString, useSingletonLifetime, maxConnectionRetries);
                           services.AddMembershipSubscriptionPostgreSqlStorage(pgSqlConnectionString, useSingletonLifetime, maxConnectionRetries);
                           services.AddStripeIntegrationPostgreSqlStorage(pgSqlConnectionString, useSingletonLifetime, maxConnectionRetries);

                           break;

                       case "mysql":
                           var mySqlConnectionString = config["DataSettings:MySqlConnectionString"];
                           services.AddCloudscribeCoreEFStorageMySql(mySqlConnectionString, useSingletonLifetime, maxConnectionRetries);
                           services.AddMembershipSubscriptionStorageMySql(mySqlConnectionString, useSingletonLifetime, maxConnectionRetries);
                           services.AddStripeIntegrationStorageMySql(mySqlConnectionString, useSingletonLifetime, maxConnectionRetries);

                           break;

                       case "mssql":
                       default:
                           var msSqlConnectionString = config["DataSettings:MsSqlConnectionString"];
                           services.AddCloudscribeCoreEFStorageMSSQL(msSqlConnectionString, useSingletonLifetime, maxConnectionRetries);
                           services.AddMembershipSubscriptionStorageMSSQL(msSqlConnectionString, useSingletonLifetime, maxConnectionRetries);
                           services.AddStripeIntegrationStorageMSSQL(msSqlConnectionString, useSingletonLifetime, maxConnectionRetries);

                           break;
                   }

                   services.AddSingleton<SubscriptionStatusService>();
                   services.AddSingleton<MembershipRoleManager>();

                   services.AddSingleton<ICheckRenewingSubscriptionStatus, StripeSubscriptionStatusChecker>();

                   services.AddSingleton<IRoleRemovalTask, RoleRemovalTask>();
                   services.Configure<StripeSettingsConfig>(config.GetSection("StripeSettingsConfig"));
                   services.AddSingleton<IStripeSettingsProvider, StripeConfigSettingsResolver>();

                   services.AddSingleton<StripeUserService>();
                   services.AddSingleton<StripeApiService>();


               })
               
               ;

            return hostBuilder;

        }
    }
}
