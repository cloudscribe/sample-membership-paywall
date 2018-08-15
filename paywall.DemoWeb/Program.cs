using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace paywall.DemoWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = BuildWebHost(args);
            
            using (var scope = host.Services.CreateScope())
            {
                var scopedServices = scope.ServiceProvider; 
                try
                {
                    EnsureDataStorageIsReady(scopedServices);

                }
                catch (Exception ex)
                {
                    var logger = scopedServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while migrating the database.");
                }
            }

            var env = host.Services.GetRequiredService<IHostingEnvironment>();
            var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
            var config = host.Services.GetRequiredService<IConfiguration>();
            ConfigureLogging(env, loggerFactory, host.Services, config);

            host.Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();

        private static void EnsureDataStorageIsReady(IServiceProvider scopedServices)
        {
            CoreEFStartup.InitializeDatabaseAsync(scopedServices).Wait();
            SimpleContentEFStartup.InitializeDatabaseAsync(scopedServices).Wait();
            LoggingEFStartup.InitializeDatabaseAsync(scopedServices).Wait();
            EmailQueueDatabase.InitializeDatabaseAsync(scopedServices).Wait();
            EmailTemplateDatabase.InitializeDatabaseAsync(scopedServices).Wait();
            MembershipDatabase.InitializeDatabaseAsync(scopedServices).Wait();

            StripeDatabase.InitializeDatabaseAsync(scopedServices).Wait();
        }

        private static void ConfigureLogging(
            IHostingEnvironment env,
            ILoggerFactory loggerFactory,
            IServiceProvider serviceProvider,
            IConfiguration config
            )
        {
            var dbLoggerConfig = config.GetSection("DbLoggerConfig").Get<cloudscribe.Logging.Models.DbLoggerConfig>();
            LogLevel minimumLevel;
            string levelConfig;
            if (env.IsProduction())
            {
                levelConfig = dbLoggerConfig.ProductionLogLevel;
            }
            else
            {
                levelConfig = dbLoggerConfig.DevLogLevel;
            }
            switch (levelConfig)
            {
                case "Debug":
                    minimumLevel = LogLevel.Debug;
                    break;

                case "Information":
                    minimumLevel = LogLevel.Information;
                    break;

                case "Trace":
                    minimumLevel = LogLevel.Trace;
                    break;

                default:
                    minimumLevel = LogLevel.Warning;
                    break;
            }

            // a customizable filter for logging
            // add exclusions in appsettings.json to remove noise in the logs
            bool logFilter(string loggerName, LogLevel logLevel)
            {
                if (dbLoggerConfig.ExcludedNamesSpaces.Any(f => loggerName.StartsWith(f)))
                {
                    return false;
                }

                if (logLevel < minimumLevel)
                {
                    return false;
                }

                if (dbLoggerConfig.BelowWarningExcludedNamesSpaces.Any(f => loggerName.StartsWith(f)) && logLevel < LogLevel.Warning)
                {
                    return false;
                }
                return true;
            }

            loggerFactory.AddDbLogger(serviceProvider, logFilter);
        }

    }


}
