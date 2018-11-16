# Email Queue Processor Console App

This project is a .NET Core console application designed to be run as a Windows Scheduled Task or as a CRON task on Linux.
This console app can process the email queue for sending renewal reminder emails for cloudscribe Membership Paywall. 

If your hosting doesn't provide a way to run scheduled tasks, the task can be run from within the web application using Hangfire which
enables CRON like task scheduling and background thread processing for web applcations

## Configuration

See the settings in appsettings.json, it is best to copy that file and name it appsettings.Development.json, that file is gitgnored in this solution to prevent putting credentiials in the code repository.
For production deployment you should create appsettings.Production.json

Things you need to configure:

* If you are going to use scheduled tasks, it is important to disable the Hangfire Service in the paywall.DemoWeb so that there are not 2 processes trying to run the tasks. Edit the appsettings.Development.json file in the web project and set "EnableHangfireService" to false before runnning the windows service.
* a connection string for an MS SqlServer, MySql, or PostgreSql database and set the DbPlatform setting accordingly.
* Email Settings using either SMTP, SendGrid, Mailgun, or Elastic Email
* other option settings seen there

### Running the Task

For testing, you can just make the this project the Startup application and run it in debug mode by hitting the play button in Visual Studio.

## How to install as a scheduled Task on Windows.

1. Create a folder where you want the task to be deployed
2. Rebuild the solution in Release mode
3. Copy all the files from SolutionFolder/EmailQueueProcessor.TaskConsole/bin/Release into the deployment folder
4. Edit the file appsettings.json file or override it with appsettings.Production.json to set the connection string and other settings for the service, ie email provider and settings either smtp, SendGrid, MailGun, or Elastic Email can be used to send the mebership reminder notifications, log level, whether to log to the Windows Event Log etc. For production I recommend set the LogLevel to Information and set LogToWindowsEventLog as true.
5. Open the Windows Task Scheduler from administrative tools.
6. I recommend create a new folder in the left pane named CustomTasks or whatever you want, then right click the folder and choose "Create Task"
7. Give the task a meaning full name like "cloudscribe Email Queue Processor"
8. In the Actions tab, click "New...", then for "Program/Script" enter dotnet
9. In the arguments type EmailQueueProcessor.TaskConsole.dll
10. In the start in put the full folder path where you deployed in step 3
11. On the General tab you may wish to set the user for the task to run as, NetworkService account for example
12. On the triggers tab you can click new then setup a schedule, you could run the email queue task every 3 or 5 minutes or whatever you decide is a short enough window
13. On the Settings tab at the bottom, make sure it is set to Do not start a new instance if the task is running
14. Look around and set other settings as you see fit.

## Have Questions or Feedback?

Visit our gitter chat web page:
[![Join the chat at https://gitter.im/joeaudette/cloudscribe](https://badges.gitter.im/joeaudette/cloudscribe.svg)](https://gitter.im/joeaudette/cloudscribe?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)