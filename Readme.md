
Solution
========
- Create the blank solution.
- Add the ASP.Net Core MVC Web App project (MvcFront).
- Add the ASP.Net Core Web Api project (OrdersApi).
- Add the ASP.Net Core Web Api project (FacesApi).

MvcFront
========
- Change the launchSettings.json file to run it as a Kestrel (console app) at port 5002.
- Add the Dapr.AspNetCore NuGet package (v1.7.0).
- Register Dapr in Startup.ConfigureServices() method.
- Add action methods in HomeController.
- Add UploadDataCommand model and OrderReceivedEvent classes.
- Code action methods.
- Add the views and update the shared layout page.
- Make sure docker desktop and all Dapr containers are running before running.
- Specify the Dapr components folder using '--components-path' directive when running (see Dapr Components section).
	- dapr run --app-id mvcfront --app-port 5002 --dapr-http-port 50002 dotnet run --components-path "..\components"

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
- 'docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Password1!" -p 1455:1433 --name DaprOrderSql -d mcr.microsoft.com/mssql/server:2019-latest'.
- Try connecting to SQL Server on container using Sql Server Management Studio (SSMS).
- Set "OrdersApi" as the startup project. Open Nuget Package Manager Console, set "OrdersApi" as the default project and run the following command.
- 'add-migration initial -output Persistence/Migrations'.
- Notice the migration scripts inside the 'Persistence/Migrations' folder.
- Run 'update-database' command and make sure there are no errors. Then check SSMS whether the tables are created now.
- Add an empty API controller called 'OrdersController' and remove the default route.
- Add a folder called 'Commands' and create OrderReceivedCommand which corresponds to OrderReceivedEvent in MvcFront project.
- Add a folder named 'Events' and create OrderRegisteredEvent class.
- Implement the OrderReceived() action to dequeue the OrderReceivedEvent, persist/register it and queue the OrderRegisteredEvent.
- Run the following command to startup the Orders Api.
	- dapr run --app-id orderapi --app-port 5003 --dapr-http-port 50003 dotnet run --components-path "..\components"
- Once FacesApi is done upto queueing OrderProcessedEvent, we can implement the OrderProcessed() action to update the order status to complete.

FacesApi
========
- Change the launchSettings.json file to run it as a Kestrel (console app) at port 5004.
- Add the Dapr.AspNetCore NuGet package (v1.7.0).
- Add the SixLabors.ImageSharp NuGet package (v2.1.3).
- Add the Microsoft.Azure.CognitiveServices.Vision.Face Nuget package (v2.8.0-preview.2).
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
- Implement the Cron() action to retrieve state store object, process it with Azure Faces and queue the OrderProcessedEvent.
- Run the following command to startup the Faces Api.
	- dapr run --app-id facesapi --app-port 5004 --dapr-http-port 50004 dotnet run --components-path "..\components"
- Connect interactively to redis terminal to view/edit/delete state store objects (see Interactive Commands).

Dapr Components
===============
- Create a new "components" folder where the solution file is located.
- Copy pubsub.yaml config file from '%userprofile%\.dapr\components' folder. Make sure that the 'name' match with code wherever 'pubsubName' is used to queue events.
- Copy statestore.yaml config file from '%userprofile%\.dapr\components' folder. Make sure that the 'name' match with code wherever 'storeName' is used to store/retrieve state.
- Draft binding-cron.yaml config file to specify CRON expresion to fire the Cron() action scheduled. Make sure that the 'name' match with code with template specified in [HttpPost] attribute of Cron() action.

Interactive Commands
====================
- Run the following command to interactively connect to redis terminal. Make sure dapr_redis is running in Docker Desktop.
	- 'docker run --rm -it --link dapr_redis redis redis-cli -h dapr_redis'
- Redis commands 
	- 'keys *' - Get all keys.
	- 'hget key data' - Get values by the key.
	- 'del key' - Delete orderList by the key (and values).
	
Azure Resources
===============
- Create an Azure Face resource on your Azure subscription.
- 