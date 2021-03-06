# cloudscribe Membership Paywall Demo App

This repository contains a reference web application to demonstate [cloudscribe Membership Paywall](https://www.cloudscribe.com/products/cloudscribe-membership-paywall). There are background tasks that are run from the web application. These background tasks handle sending renewal reminders for memberships that are expiring soon or have expired. There is also a background task to remove users from membership granted roles when the membership expires. 


cloudscribe Membership Paywall is a commercial add on feature for websites and applications built on [cloudscribe Core](https://github.com/cloudscribe/cloudscribe).
It is free to try but has alerts indiciating it is a free trial and the alerts will be shown every few requests until a license file is installed. If you are new to cloudscribe, please see the [Introduction](https://www.cloudscribe.com/docs/introduction).

cloudscribe Membership Paywall uses [Stripe](https://stripe.com) for online payments with full support for recurring subscriptions so users can be charged automatically and membership renewal can happen automatically. Users can of course opt out of automatic renewal at any time. Stripe has zero setup costs, and no monthly fees, it only has reasonable per transaction fees. 

Note that cloudscribe StripeIntegration is an independent product purchased separately but is required for cloudscribe Membership inorder to process payments, and is included in this demo.

## Prerequisites

DotNet Core SDK latest version and/or Visual Studio 2017 with the latest updates and ASP.NET Core Workload

## Testing the Web App

This sample uses Microsoft Sql Server. Other platforms including PostgreSql and MySql are supported but to try this sample you need either Sql Server or Sql Server Express.

1. Copy the appsettings.json file in the paywall.DemoWeb project as appsettings.Development.json - this file is gitignored in our repository to prevent accidently putting credentials in the repository.
2. Create a Sql Server database and set the connection string in appsettings.Development.json
3. To test the membership email reminders you also need to configure something to send email, in the appsettings.Development.json file you will see empty settings for Smtp, SendGrid, Mailgun, and Elastic Email. You can use any one of those, the system will use the first one it finds that has suppplied credentials, it is best to only populate one of those options.
4. Optionally if you wish to use Stripe for collecting payments online, you need to populate these settings in appsettings.Development.json using sandbox credentials for testing:

        `"StripeSettingsConfig": {
           "Settings": [
            {
              "TenantId": "*",
              "UseProductionApi": false,
              "ProductionPublicKey": "",
              "ProductionSecretKey": "",
              "ProductionWebHookSecret": "",
              "SandboxPublicKey": "",
              "SandboxSecretKey": "",
              "SandboxWebHookSecret": "",
              "CurrencyCode": "USD"
              }
            ]
          },`
5. Rebuild the solution, this may take a few minutesd the first time, since it needs to restore packages.
6. Right click the paywall.DemoWeb project node in Solution  Explorer and choose "View in browser".
7. You can login with admin@admin.com and the password admin - after you login a new Administration link will appear in the menu, click that link to see the Administration Menu
8. Go to Administration > Role Management and create a new role named "PremiumMembers" or whatever you want to name it.
9. Go to Administration > Membership Management > Membership Levels, create a new Membership Level and use the role selector link to open a dialog of roles, choose the role you just created. It is recommended to name the level something similar to the role, such as "Premium Membership"
10. Create a reminder email template under Administration > Membership Management > Reminder Templates. For test purposes, set the Headline to "Only 1 Day Left!", the Subject to "Don't let your Membership Expire!". For the Html Content use something like this, make the renew text a link to /membership:

  >Dear *{firstName*} *{lastName*},
  >
  >    Your membership expires on *{endDate*}, please renew before your access expires.' 


11. Go to Administration > Membership Management > Membership Products and create a new product. For testing purposes set theTitle as "30 Day Free Trial for New Members!", the Description as "Get 30 days of full access to our content with a free trial.", choose the Membership Level that you created previously. Set the duration in Days to 30, the price as 0 and check the box "Only Available for New Members", then save it.
12. After saving the product a new section will appear at the bottom of the product edit page for "Renewal Reminders". Click the "Add New" link. Use the previouslty created product and email template, set the name to "1 Day Left", for Date Target use Days Before Expiration, for Days enter 1, and save it.
13. Assuming all the steps above have been complted it is time to log out as administrator and register as a new user with a valid email address using the register link at the top of the page. After you register login as the new user.
14. You should see an alert indicating you don't have an active membership, click the link in the alert.
15. Click the Buy Now button next to the free trial and complete the form to get you membership.
16. Note that it is also possible to create membership tickets under Administration > Membership Management > Order Entry
17. Note there is a separate section in the menu for Administration > Stripe Administration, which lets you see your stripe customers, subscriptions, charges etc using the Stripe API.

## Testing Reminders

Now that you have created a membership ticket to test with you can edit it to adjust the end date to meet the requirments of a reminder. You can login as admin again and find the existing membership ticket under Administration > Membership Management > Order Search.

The reminders task is designed to only run once a day so that no user gets duplicate reminders. The configuration can be seen in appsettings.json:

"SendRemindersBackgroundTaskOptions": {
    "CronSchedule": "30 9 * * *",
    "TaskSleepTimeInMilliseconds": 3000
  },
  "RoleRemovalBackgroundTaskOptions": {
    "CronSchedule": "30 23 * * *",
    "TaskSleepTimeInMilliseconds": 3000
  },
  
The tasks above can be scheduled using cron format as seen here: https://github.com/atifaziz/NCrontab

The email queue is processed every 3 minutes.

Note also that reminders are NOT sent to users with automatic renewal enabled, only users who opt out of automatic renewals get reminders to renew their membership.

## Testing/Verifying that the granted role is removed when membership expires.

Verify that a user for a given membership ticket is currently in the granted role under Administration > Role Management

Edit the membership ticket end date to make it expired yesterday or any day not older than 30 days ago.

You should be able to verify after the task has run that the user has been removed from the role.


## Additional Guidance

Review the settings under Administration > Membership Management > Settings

That is where you can configure alerts shown within the site to encourage membership. You can edit content that will appear on the  /membership page and configure whether membership products and current membership is shown there.

It is a good idea to test the system initially to make sure it is setup to meet your needs. Once you have real members with real membership tickets you probably should not manually trigger the tasks, otherwise users would get duplicate email notifications.

Note that the reminder email messages in confirmance with email laws also has an opt out link in the footer to make it easy for users to opt out of email reminders for the rest of the duration of a given membership.

Note also that in settings you can define roles to not alert so that administrators don't get prompted to purchase membership for example.

## Email Templates

The Reminder Templates generate html strings from the content entered and using a Razor View and layout. The razor files can be found under Views/Shared and can be customized. However if you make changes in the Razor files, then you need to save the templates again to generate the new template string, there is no way to automate that.

If you have questions please visit our community forum https://www.cloudscribe.com/forum


