// Author:					Joe Audette
// Created:					2018-06-17
// Last Modified:			2018-06-17
// 

using Autofac;
using Autofac.Extensions.DependencyInjection;
using cloudscribe.EmailQueue.HangfireIntegration;
using cloudscribe.EmailQueue.Models;
using cloudscribe.EmailQueue.Services;
using cloudscribe.Membership.HangfireIntegration;
using cloudscribe.Membership.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskProcessingService
{
    public static class DI
    {
        public static IContainer BuildContainer(IConfiguration config)
        {
            var services = new ServiceCollection();
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

            services.AddCloudscribeEmailSenders(config);

            var connectionString = config["AppSettings:ConnectionString"];

            services.AddEmailTemplateStorageMSSQL(connectionString);
            services.AddEmailQueueStorageMSSQL(connectionString);
            services.AddScoped<IEmailQueueProcessor, HangFireEmailQueueProcessor>();
            services.AddEmailQueueServices(config);

            services.AddMembershipSubscriptionStorageMSSQL(connectionString);
            services.AddScoped<IRoleRemovalTask, HangfireRoleRemovalTask>();
            services.AddScoped<ISendRemindersTask, HangfireSendRemindersTask>();







            // create the Autofac DI container and populate with services from Microsoft DI ServiceCollection
            var builder = new ContainerBuilder();
            builder.Populate(services);
            var container = builder.Build();
            return container;
        }

    }
}
