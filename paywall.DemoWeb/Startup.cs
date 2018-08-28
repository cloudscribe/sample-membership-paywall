using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using paywall.DemoWeb.Config;
using System;


namespace paywall.DemoWeb
{
    public class Startup
    {
        public Startup(
            IConfiguration configuration, 
            IHostingEnvironment env,
            ILogger<Startup> logger
            )
        {
            _configuration = configuration;
            _environment = env;
            _log = logger;

            _sslIsAvailable = _configuration.GetValue<bool>("AppSettings:UseSsl");
            _enableHangfireService = _configuration.GetValue<bool>("AppSettings:EnableHangfireService");
            _enableHangfireDashboard = _configuration.GetValue<bool>("AppSettings:EnableHangfireDashboard");
        }

        private readonly IConfiguration _configuration;
        private readonly IHostingEnvironment _environment;
        private readonly ILogger _log;
        private readonly bool _sslIsAvailable;
        private readonly bool _enableHangfireService = true;
        private readonly bool _enableHangfireDashboard = true;

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //// **** VERY IMPORTANT *****
            // This is a custom extension method in Config/DataProtection.cs
            // These settings require your review to correctly configur data protection for your environment
            services.SetupDataProtection(_configuration, _environment);
            
            services.AddAuthorization(options =>
            {
                //https://docs.asp.net/en/latest/security/authorization/policies.html
                //** IMPORTANT ***
                //This is a custom extension method in Config/Authorization.cs
                //That is where you can review or customize or add additional authorization policies
                options.SetupAuthorizationPolicies();

            });

            //// **** IMPORTANT *****
            // This is a custom extension method in Config/CloudscribeFeatures.cs
            services.SetupDataStorage(_configuration);
            
            //*** Important ***
            // This is a custom extension method in Config/CloudscribeFeatures.cs
            services.SetupCloudscribeFeatures(_configuration);

            //*** Important ***
            // This is a custom extension method in Config/Localization.cs
            services.SetupLocalization();

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = cloudscribe.Core.Identity.SiteCookieConsent.NeedsConsent;
                options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.None;
                options.ConsentCookie.Name = "cookieconsent_status";
            });

            services.Configure<Microsoft.AspNetCore.Mvc.CookieTempDataProviderOptions>(options =>
            {
                options.Cookie.IsEssential = true;
            });

            //*** Important ***
            // This is a custom extension method in Config/RoutingAndMvc.cs
            services.SetupMvc(_sslIsAvailable);


        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IServiceProvider serviceProvider,
            IApplicationBuilder app, 
            IHostingEnvironment env,
            ILoggerFactory loggerFactory,
            IOptions<cloudscribe.Core.Models.MultiTenantOptions> multiTenantOptionsAccessor,
            IOptions<RequestLocalizationOptions> localizationOptionsAccessor
            )
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/oops/error");
            }
            
            app.UseStaticFiles();
            app.UseCloudscribeCommonStaticFiles();
            app.UseCookiePolicy();

            //app.UseSession();

            app.UseRequestLocalization(localizationOptionsAccessor.Value);

            var multiTenantOptions = multiTenantOptionsAccessor.Value;

            app.UseCloudscribeCore(
                    loggerFactory,
                    multiTenantOptions,
                    _sslIsAvailable);

            if (_enableHangfireDashboard)
            {
                app.UseHangfireDashboard("/tasks", new DashboardOptions
                {
                    Authorization = new[] { new HangFireAuthorizationFilter() }
                });
            }

            if (_enableHangfireService)
            {
                var options = new BackgroundJobServerOptions
                {
                    // This is the default value
                    //WorkerCount = Environment.ProcessorCount * 5
                    WorkerCount = 5
                };
                app.UseHangfireServer(options);

                GlobalConfiguration.Configuration.UseActivator(new cloudscribe.EmailQueue.HangfireIntegration.HangfireActivator(serviceProvider));

                RecurringJob.RemoveIfExists("email-processor");
                RecurringJob.AddOrUpdate<cloudscribe.EmailQueue.Models.IEmailQueueProcessor>("email-processor", 
                    mp => mp.StartProcessing(), 
                    Cron.MinuteInterval(10)); //every 10 minutes

                RecurringJob.RemoveIfExists("expired-membership-processor");
                RecurringJob.AddOrUpdate<cloudscribe.Membership.Models.IRoleRemovalTask>("expired-membership-processor", 
                    x => x.RemoveExpiredMembersFromGrantedRoles(), 
                    Cron.Daily(23)); //11pm

                RecurringJob.RemoveIfExists("membership-reminder-email-processor");
                RecurringJob.AddOrUpdate<cloudscribe.Membership.Models.ISendRemindersTask>("membership-reminder-email-processor", 
                    x => x.SendRenewalReminders(), 
                    Cron.Daily(7)); //7am


            }


            app.UseMvc(routes =>
            {
                var useFolders = multiTenantOptions.Mode == cloudscribe.Core.Models.MultiTenantMode.FolderName;
                //*** IMPORTANT ***
                // this is in Config/RoutingAndMvc.cs
                // you can change or add routes there
                routes.UseCustomRoutes(useFolders);
            });
   
        }

        
        
    }
}