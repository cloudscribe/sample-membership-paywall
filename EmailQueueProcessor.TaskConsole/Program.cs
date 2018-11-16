using cloudscribe.Email;
using cloudscribe.Email.ElasticEmail;
using cloudscribe.Email.Mailgun;
using cloudscribe.Email.Senders;
using cloudscribe.Email.SendGrid;
using cloudscribe.Email.Smtp;
using cloudscribe.EmailQueue.Models;
using cloudscribe.EmailQueue.Services;
using cloudscribe.Membership.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;

namespace EmailQueueProcessor.TaskConsole
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
                    EmailQueueDatabase.InitializeDatabaseAsync(scopedServices).Wait();
                    MembershipDatabase.InitializeDatabaseAsync(scopedServices).Wait();
                    //EmailListDatabase.InitializeDatabaseAsync(scopedServices).Wait();
                }
                catch (Exception ex)
                {
                    var logger = scopedServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while migrating the database.");
                }
            }

            // note this sample uses email queue for membership reminder emails
            // if you have other things pushing messages into the queue such as
            // cloudscribe.EmailList, you should only have one task processing the queue
            // therefore you would add any other IEmailRecipientProvider implementations so that one and only one scheduled task can 
            // handle processing the queue
            var job = host.Services.GetRequiredService<IEmailQueueProcessor>();

            job.StartProcessing().Wait();

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
                           services.AddEmailQueuePostgreSqlStorage(pgSqlConnectionString, useSingletonLifetime, maxConnectionRetries);
                           //services.AddEmailListPostgreSqlStorage(pgSqlConnectionString, useSingletonLifetime, maxConnectionRetries);
                           services.AddMembershipSubscriptionPostgreSqlStorage(pgSqlConnectionString, useSingletonLifetime, maxConnectionRetries);

                           break;

                       case "mysql":
                           var mySqlConnectionString = config["DataSettings:MySqlConnectionString"];
                           services.AddEmailQueueStorageMySql(mySqlConnectionString, useSingletonLifetime, maxConnectionRetries);
                           //services.AddEmailListStorageMySql(mySqlConnectionString, useSingletonLifetime, maxConnectionRetries);
                           services.AddMembershipSubscriptionStorageMySql(mySqlConnectionString, useSingletonLifetime, maxConnectionRetries);

                           break;

                       case "mssql":
                       default:

                           var msSqlConnectionString = config["DataSettings:MsSqlConnectionString"];
                           services.AddEmailQueueStorageMSSQL(msSqlConnectionString, useSingletonLifetime, maxConnectionRetries);
                           //services.AddEmailListStorageMSSQL(msSqlConnectionString, useSingletonLifetime, maxConnectionRetries);
                           services.AddMembershipSubscriptionStorageMSSQL(msSqlConnectionString, useSingletonLifetime, maxConnectionRetries);

                           break;
                   }

                   services.AddSingleton<ISmtpOptionsProvider, ConfigSmtpOptionsProvider>();
                   services.Configure<SmtpOptions>(config.GetSection("SmtpOptions"));
                   services.AddSingleton<IEmailSender, SmtpEmailSender>();

                   services.AddSingleton<ISendGridOptionsProvider, ConfigSendGridOptionsProvider>();
                   services.Configure<SendGridOptions>(config.GetSection("SendGridOptions"));
                   services.AddSingleton<IEmailSender, SendGridEmailSender>();

                   services.AddSingleton<IMailgunOptionsProvider, ConfigMailgunOptionsProvider>();
                   services.Configure<MailgunOptions>(config.GetSection("MailgunOptions"));
                   services.AddSingleton<IEmailSender, MailgunEmailSender>();

                   services.AddSingleton<IElasticEmailOptionsProvider, ConfigElasticEmailOptionsProvider>();
                   services.Configure<ElasticEmailOptions>(config.GetSection("ElasticEmailOptions"));
                   services.AddSingleton<IEmailSender, ElasticEmailSender>();

                   services.AddSingleton<IEmailSenderResolver, ConfigEmailSenderResolver>();

                   services.AddSingleton<IServiceClientProvider, DefaultServiceClientProvider>();

                   services.AddSingleton<IEmailQueueProcessor, DefaultEmailQueueProcessor>();
                   services.AddSingleton<IReplaceEmailMessageTokens, DefaultEmailTokenReplacer>();
                   services.AddSingleton<IEmailQueueItemSender, DefaultEmailQueueItemSender>();
                   services.AddSingleton<IRecipientCsvParser, RecipientCsvParser>();

                   //services.AddSingleton<IEmailRecipientProvider, EmailListRecipientProvider>();
                   services.AddSingleton<IEmailRecipientProvider, ReminderRecipientProvider>();


               })
              
               ;

            return hostBuilder;

        }
    }
}
