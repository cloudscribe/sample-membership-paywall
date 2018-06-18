# Task Processing Service

This project is a windows service example for running background tasks for cloudscribe Membership Paywall. The tasks can be run from within the web application using Hangfire, but this example shows how you can also run the tasks from a Windows service which may be more reliable than running tasks on background threads in a web application. If you are new to cloudscribe, please see the [Introduction](https://www.cloudscribe.com/docs/introduction).

This service is built using [Topshelf](http://topshelf-project.com/) and [Hangfire](https://www.hangfire.io/)

## About Topshelf

[Topshelf](http://topshelf-project.com/) is a framework for hosting services written using the .NET framework. The creation of services is simplified, allowing developers to create a simple console application that can be installed as a service using Topshelf. The reason for this is simple: It is far easier to debug a console application than a service. And once the application is tested and ready for production, Topshelf makes it easy to install the application as a service.

## About Hangfire

[Hangfire](https://www.hangfire.io/) provides an easy way to perform background processing in .NET and .NET Core applications with support for Fire and Forget jobs, as well as Scheduled and Recurring Jobs.

## Prerequisites

* Visual Studio 2017 with latest updates.
* .NET 4.6.1 or higher installed

## Configuration

See the settings in appsettings.json you need to configure:
* If you are going to use the windows service to run the tasks, it is important to disable the Hangfire Service in the paywall.DemoWeb so that there are not 2 processes trying to run the tasks. Edit the appsettings.Development.json file in the web project and set "EnableHangfireService" to false before runnning the windows service.
* a connection string for an MS SqlServer database
* Email Settings using either SMTP, SendGrid, Mailgun, or Elastic Email
* other option settings seen there

For testing in a development environment you should copy the appsettings.json file as appsettings.Development.json. This file is only used in debug mode and will override the main appsettings.json file. In our git repository we have that file gitignored to prevent credentials from going into the repository.

For production environments it is reomeended to copy the appsettings.json file as appsettings.Production.json

### Running the service

For testing, you can just make the TaskProcessingService the Startup application and run it in debug mode by hitting the play button in Visual Studio.

Note that Hangfire has a set of tables it will create, you can use the same database or a different one for the Hangfire data, this example code just uses the same database as the demo web app built with cloudscribe components.


## How to install this service as a windows service

1. Create a folder where you want the service to be deployed
2. Rebuild the solution in Release mode
3. Copy all the files from SolutionFolder/TaskProcessingService/bin/Release into the deployment folder
4. Edit the file appsettings.json file or override it with appsettings.Production.json to set the connection string and other settings for the service, ie email provider and settings either smtp, SendGrid, MailGun, or Elastic Email can be used to send the mebership reminder notifications, log level, whether to log to the Windows Event Log etc. For production I recommend set the LogLevel to Information and set LogToWindowsEventLog as true.
5. Open a command window or powershell window as Administrator and navigate to the deployment folder.
6. Type the command .\TaskProcessingService.exe install
7. Open the Windows Service console and verify that you see Task Processing Service - cloudscribe
8. You can start the service manually there. In production use it typically should be set to Automatic (Delayed Start).

I think it is a good idea to verify that the service can write to the Windows Application log. By setting a bad connection string and then try to start the service, it will fail to start with "Access Denied", confirm that the error details can be found in the log using Event Viewer.

## How to uninstall the service

1. Stop the service in windows service console
2. Open a command window or powershell window as Administrator and navigate to the deployment folder.
3. Type the command .\TaskProcessingService.exe uninstall
4. Close and re-open the windows service console to verify the service was removed.

See also the [Topshelf command line reference](http://docs.topshelf-project.com/en/latest/overview/commandline.html)

## Some Developer Tips

If you use this project as a sample to make your own tasks with a windows service, and you find that your tasks aren't working and if it doesn't hit breakpoints in your task code during debug, it usually means it failed to resolve some dependency needed by your task. Unfortunately you won't see any errors about that in the console window, it will just fail silently. So double check that you have registered all the needed dependencies for your tasks. Note that although this console app requires the full .NET Framework, you can write your tasks in netstandard class libraries and reference those from the console app. Another thing I've noticied is for example some dependency is missing and you try to run the task 5 times and it fails silently each time, then you fix the dependency, it seems that Hangfire remembers the failed attempts and runs your tasks 5 times to complete the previous 5 times when it failed.

## Have Questions or Feedback?

Visit our gitter chat web page:
[![Join the chat at https://gitter.im/joeaudette/cloudscribe](https://badges.gitter.im/joeaudette/cloudscribe.svg)](https://gitter.im/joeaudette/cloudscribe?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)