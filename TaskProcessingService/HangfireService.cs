// Author:					Joe Audette
// Created:					2018-06-17
// Last Modified:			2018-06-17
// 

using Autofac;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Topshelf;

namespace TaskProcessingService
{
    public class HangfireService : ServiceControl
    {
        private BackgroundJobServer _server;
        private IConfiguration _config;
        private ILogger _log;
        private IContainer _container;

        public HangfireService(IConfiguration config, ILogger logger, IContainer container)
        {
            _config = config;
            _log = logger;
            _container = container;
        }

        public bool Start(HostControl hostControl)
        {
            try
            {
                var connectionString = _config["AppSettings:ConnectionString"];
                
                //https://stackoverflow.com/questions/27961210/hangfire-dependency-injection-lifetime-scope?utm_medium=organic&utm_source=google_rich_qa&utm_campaign=google_rich_qa
                //this makes sure depencies are scoped to each run of the job
                //and disposed after each run
                GlobalConfiguration.Configuration.UseAutofacActivator(_container, false);
                JobStorage.Current = new SqlServerStorage(connectionString);

                _server = new BackgroundJobServer();

                BackgroundJob.Enqueue(() => Console.WriteLine("Setting up tasks"));

                RecurringJob.RemoveIfExists("email-processor");
                var emailQueueProcessorMinuteInterval = _config.GetValue<int>("AppSettings:EmailQueueProcessorMinuteInterval");
                RecurringJob.AddOrUpdate<cloudscribe.EmailQueue.Models.IEmailQueueProcessor>("email-processor", 
                    mp => mp.StartProcessing(),
#if DEBUG
                    Cron.MinuteInterval(1)
#else
                    Cron.MinuteInterval(emailQueueProcessorMinuteInterval)
#endif
                    );

                RecurringJob.RemoveIfExists("expired-membership-processor");
                var removeExpiredMembersFromGrantedRolesHourOfDayToRun = _config.GetValue<int>("AppSettings:RemoveExpiredMembersFromGrantedRolesHourOfDayToRun");
                RecurringJob.AddOrUpdate<cloudscribe.Membership.Models.IRoleRemovalTask>("expired-membership-processor", 
                    x => x.RemoveExpiredMembersFromGrantedRoles(),
#if DEBUG
                    Cron.MinuteInterval(3)
#else
                    Cron.Daily(removeExpiredMembersFromGrantedRolesHourOfDayToRun)
#endif             
                    );

                RecurringJob.RemoveIfExists("membership-reminder-email-processor");
                var sendRenewalRemindersHourOfDayToRun = _config.GetValue<int>("AppSettings:SendRenewalRemindersHourOfDayToRun");
                RecurringJob.AddOrUpdate<cloudscribe.Membership.Models.ISendRemindersTask>("membership-reminder-email-processor", 
                    x => x.SendRenewalReminders(),
#if DEBUG
                    Cron.MinuteInterval(2)
#else
                    Cron.Daily(sendRenewalRemindersHourOfDayToRun)
#endif
                    );

                return true;
            }
            catch (Exception ex)
            {
                _log.LogCritical($"{ex.Message} : {ex.StackTrace}");

                return false;
            }

        }

        public bool Stop(HostControl hostControl)
        {
            _server.Dispose();
            return true;
        }



    }
}
