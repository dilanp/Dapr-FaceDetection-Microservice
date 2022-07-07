
Solution
========
- Create the blank solution.
- Add the ASP.Net Core MVC Web App project (MvcFront).
- Add the ASP.Net Core Web Api project (OrdersApi).
- Add the ASP.Net Core Web Api project (FacesApi).
- Add the ASP.Net Core Web Api project (NotificationApi).

MvcFront
========
- Change the launchSettings.json file to run it as a Kestrel (console app) at port 5002.
- Add the Dapr.AspNetCore NuGet package (v1.7.0).
- Register Dapr in Startup.ConfigureServices() method.
- Add Dapr pubsub binding for message queueing.
- Add action methods in HomeController.
- Add UploadDataCommand model and OrderReceivedEvent classes.
- Code action methods.
- Add the views and update the shared layout page.
- Make sure docker desktop and all Dapr containers are running before running.
- Specify the Dapr components folder using '--components-path' directive when running (see Dapr Components section).
	- dapr run --app-id mvcfront-service --app-port 5002 --dapr-http-port 50002 dotnet run --components-path '..\components'
- Once GetAllOrdersAsync() action is added to orders api add new view to show all order details and add a link to it.
- Copy the Order and Status models from orders api into Models folder.
- Add a folder named "Services" and code OrderClient to fetch orders from orders api.
- Add singleton dependancy injection support for OrderClient in Startup.ConfigureServices() method. 
- Inject OrderClient dependancy into HomeController and code a new action method - AllOrders().
- Add new View to show all orders and add a link to _Layout.cshtml page to get to it.

Orders Api
==========
- Change the launchSettings.json file to run it as a Kestrel (console app) at port 5003.
- Add the Dapr.AspNetCore NuGet package (v1.7.0).
- Add the SixLabors.ImageSharp NuGet package (v2.1.3).
- Add the Microsoft.EntityFrameworkCore NuGet package (v5.0.17).
- Add the Microsoft.EntityFrameworkCore.Design NuGet package (v5.0.17).
- Add the Microsoft.EntityFrameworkCore.SqlServer NuGet package (v5.0.17).
- Add the Microsoft.EntityFrameworkCore.Tools NuGet package (v5.0.17).
- Register Dapr in Startup.ConfigureServices() method.
- Add 'app.UseCloudEvents()' call in Startup.Configure() to enable middleware.
- Add 'endpoints.MapSubscribeHandler()' call in Startup.Configure() to enable middleware.
- Add required models.
- Add Entity Framework related entities into 'Persistence' folder.
- Add DB ConnectionString to the appSettings.json file (DB will be SQL Server running on a container).
- Wire up the DbContext in Startup.ConfigureServices() method.
- Register the OrderRepository in Startup.ConfigureServices() method.
- Get SQL Server running on a container. It's very important to map port 1433 to some other port to avoid screwing up the local SQL instance.
- The following command will ensure the SQL Server container matches with the connection string specified in appSettings.json file.
	- docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Password1!' -p 1455:1433 --name DaprOrderSql -d mcr.microsoft.com/mssql/server:2019-latest.
- Try connecting to SQL Server on container using Sql Server Management Studio (SSMS).
- Set "OrdersApi" as the start up project. Open Nuget Package Manager Console, set "OrdersApi" as the default project and run the following command.
- 'add-migration initial -output Persistence/Migrations'.
- Notice the migration scripts inside the 'Persistence/Migrations' folder.
- Run 'update-database' command and make sure there are no errors. Then check SSMS whether the tables are created now.
- Add an empty API controller called 'OrdersController' and remove the default route.
- Add a folder called 'Commands' and create OrderReceivedCommand which corresponds to OrderReceivedEvent in MvcFront project.
- Add a folder named 'Events' and create OrderRegisteredEvent class.
- Implement the OrderReceived() action to dequeue the OrderReceivedEvent, persist/register it and queue the OrderRegisteredEvent.
- Run the following command to startup the Orders Api.
	- dapr run --app-id ordersapi-service --app-port 5003 --dapr-http-port 50003 dotnet run --components-path '..\components'
- Once FacesApi is done upto queueing OrderProcessedEvent, we can implement the OrderProcessed() action to update the order status to "Processed".
- Once NotificationApi is done upto queueing OrderDispatchedEvent, we can implement OrderDispatched() action to update the order status to "Dispatched".
- Once all order statuses are implemented, add new GetAllOrdersAsync() action method to return all orders via OrderRepository.

FacesApi
========
- Change the launchSettings.json file to run it as a Kestrel (console app) at port 5004.
- Add the Dapr.AspNetCore NuGet package (v1.7.0).
- Add the SixLabors.ImageSharp NuGet package (v2.1.3).
- Add the Microsoft.Azure.CognitiveServices.Vision.Face Nuget package (v2.7.0-preview.1).
- Create an Azure Face resource on your Azure subscription and copy credentials into appSettings.json file.
- Add a class called AzureFaceConfiguration in project root and add properties with same name as in appSettings.json file.
- Change Startup.ConfigureServices() code to enable SixLabors to synchronously process images.
- Register Azure Face app settings through dependency injection (singleton) in Startup.ConfigureServices() method.
- Register Dapr in Startup.ConfigureServices() method.
- Add 'app.UseCloudEvents()' call in Startup.Configure() to enable middleware.
- Add 'endpoints.MapSubscribeHandler()' call in Startup.Configure() to enable middleware.
- Add an empty API controller called 'FacesController' and remove the default route.
- Add a folder called 'Commands' and create OrderRegisteredCommand which corresponds to OrderRegisteredEvent in OrdersApi project.
- Add a folder named 'Events' and create OrderProcessedEvent class.
- Implement the ProcessOrder() action to dequeue the OrderRegisteredEvent and save to state store.
- Add Dapr binding for cron expression.
- Add Dapr stateStore binding for stateful service.
- Implement the Cron() action to retrieve state store object, process it with Azure Faces and queue the OrderProcessedEvent.
- Run the following command to start up the Faces Api.
	- dapr run --app-id facesapi-service --app-port 5004 --dapr-http-port 50004 dotnet run --components-path '..\components'
- Connect interactively to redis terminal to view/edit/delete state store objects (see Interactive Commands).

NotificationApi
===============
- Change the launchSettings.json file to run it as a Kestrel (console app) at port 5004.
- Add the Dapr.AspNetCore NuGet package (v1.7.0).
- Add the SixLabors.ImageSharp NuGet package (v2.1.3).
- Register Dapr in Startup.ConfigureServices() method.
- Add 'app.UseCloudEvents()' call in Startup.Configure() to enable middleware.
- Add 'endpoints.MapSubscribeHandler()' call in Startup.Configure() to enable middleware.
- Add an empty API controller called 'NotificationController' and remove the default route.
- Add a folder called 'Commands' and create DispatchOrderCommand which corresponds to OrderProcessedEvent in OrdersApi project.
- Add a folder named 'Events' and create OrderDispatchedEvent class.
- Add a folder named 'Helpers' and create EmailUtils class. Then, code CreateEmailBody() method to generate email body.
- Add Dapr output binding for sending emails.
- Run "maildev" container to act as SMTP server for sending emails.
	- docker run -d -p 4000:80 -p 4025:25 --name fds-maildev maildev/maildev:latest
	- port 80 mapped to 4000 to see emails coming through the mail server.
	- port 25 is the SMTP port mapped to 4025.
	- container name given is "fds-maildev".
- Run the following command to start up the Faces Api.
	- dapr run --app-id notification-service --app-port 5005 --dapr-http-port 50005 dotnet run --components-path '..\components'
	
Dapr Components
===============
- Create a new "components" folder where the solution file is located.
- Copy pubsub.yaml config file from '%userprofile%\.dapr\components' folder. Make sure that the 'name' match with code wherever 'pubsubName' is used to queue events.
- Copy statestore.yaml config file from '%userprofile%\.dapr\components' folder. Make sure that the 'name' match with code wherever 'storeName' is used to store/retrieve state.
- Draft binding-cron.yaml config file to specify CRON expresion to fire the Cron() action scheduled. Make sure that the 'name' match with code with template specified in [HttpPost] attribute of Cron() action.
- Draft binding-email.yaml config file to specify output binding required to send emails.

Interactive Commands
====================
- Run => 'dapr dashboard' to reveal Dapr dashboard.
	- http://localhost:8080/overview
- Run => 'docker ps' to reveal Zipkin port.
	- http://localhost:9411/zipkin/
- Run the following command to interactively connect to redis terminal. Make sure dapr_redis is running in Docker Desktop.
	- docker run --rm -it --link dapr_redis redis redis-cli -h dapr_redis
- Redis commands 
	- 'keys *' - Get all keys.
	- 'hget key data' - Get values by the key.
	- 'del key' - Delete orderList by the key (and values).
- At the end of creating all 4 microservices create a launch.bat file (Win) to launch all of them at the same time.

The Dapr service endpoint format
================================
http://localhost:{dapr-http-port}/v1.0/invoke/{app-id}/method/{method-name}

Azure Resources
===============
- Create an Azure Face resource on your Azure subscription.

Tye Orchestration
=================
- Install Tye
	- dotnet tool install -g Microsoft.Tye --version "0.11.0-alpha.22111.1"
- Check installed Tye version
	- tye --version
- Move to the solution folder in PowerShell and issue this command to create Tye configuration file.
	- tye init
- Inspect the tye.yaml config file and make sure all 4 projects are configured there.
- Update each service in config file with protocol and port number under bindings.
- Add Sql Server database container details to the config.
- Change the DB setup in OrdersApi.Startup.ConfigureServices() method to use the above Sql Server config.
- Add a "CustomConfig" section to appsettings.Development.json to indicate whether DB migrations needs running (RunDbMigrations).
- Add an interface called IConfig in OrdersApi project and include RunDbMigrations.
- Add the Config class inherit from IConfig and get value from appsettings.Development.json.
- Add the singleton config in OrdersApi.Startup.ConfigureServices() method to fetch the "CustomConfig" settings.
- Introduce TryRunMigrations() method to run DB migrations based on the config setting and call it at the bottom of OrdersApi.Startup.Configure() method to make sure it runs at the end of the middleware pipeline and everything is initialized before that.
- Add Dapr and SEQ extensions to the tye.yaml config file.
- Please note that containers such as dapr_redis and fds-maildev will not be available through Tye dashboard because they are not part of the tye.yaml file.
- Move to the solution folder and run a Tye build to create docker images of the component setup in the tye.yaml file.
	-tye build
- If build doesn't have any errors go ahead and execute a Tye run.
	- tye run
- Tye dashboard can be accessed at the following URL.
	- http://localhost:8000/
- To debug microservices through Tye first use the command(s) at the solution root.
	- tye run --debug OrdersApi/OrderApi.csproj
	- try run --debug *
- Then in Visual Studio choose, Debug > Attach to process, select OrderApi.exe and use breakpoints to debug.