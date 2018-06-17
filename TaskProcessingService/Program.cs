// Author:					Joe Audette
// Created:					2018-06-17
// Last Modified:			2018-06-17
// 

using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Threading;
using Topshelf;

namespace TaskProcessingService
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder();
            builder.AddJsonFile("appsettings.json", optional: false);
#if DEBUG
            builder.AddJsonFile("appsettings.Development.json", optional: true);
#endif
            builder.AddJsonFile("appsettings.Production.json", optional: true);

            var config = builder.Build();

            var cultureCode = config["AppSettings:ThreadCulture"];
            if (string.IsNullOrWhiteSpace(cultureCode))
            {
                cultureCode = "en-US";
            }

            var culture = new CultureInfo(cultureCode);
            Thread.CurrentThread.CurrentCulture = culture;

            var container = DI.BuildContainer(config);

            ILogger logger = null;
            using (var scope = container.BeginLifetimeScope())
            {
                logger = scope.Resolve<ILogger<HangfireService>>();
            }

            var rc = HostFactory.Run(hostConfigurator =>
            {
                hostConfigurator.Service<HangfireService>(serviceConfigurator =>
                {
                    serviceConfigurator.ConstructUsing(() => new HangfireService(config, logger, container));

                    serviceConfigurator.WhenStarted((s, hostControl) => s.Start(hostControl));
                    serviceConfigurator.WhenStopped((s, hostControl) => s.Stop(hostControl));

                });
                hostConfigurator.RunAsNetworkService();

                hostConfigurator.SetServiceName("Task Processing Service - cloudscribe");
                hostConfigurator.SetDescription("Process tasks for cloudscribe web apps, such as email queue, membership reminders, and other tasks.");

            });

            var exitCode = (int)Convert.ChangeType(rc, rc.GetTypeCode());
            Environment.ExitCode = exitCode;

            //Console.WriteLine(connectionString);
            //Console.Read();

        }
    }
}
