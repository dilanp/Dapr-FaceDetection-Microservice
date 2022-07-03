
Solution
========
- Create the blank solution.
- Add the ASP.Net Core MVC web app (MvcFront).
- Add the ASP.Net Core Web Api (OrdersApi).

MvcFront
========
- Change the launchSettings.json file to run it as a console app at port 5002.
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
- Change the launchSettings.json file to run it as a console app at port 5003.
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
- dapr run --app-id orderms --app-port 5003 --dapr-http-port 50003 dotnet run --components-path "..\components"

Dapr Components
===============
- Create a new "components" folder where the solution file is located.
- Copy pubsub.yaml config file from '%userprofile%\.dapr\components' folder.
- Change the 'name' to match with the code in MvcFront.HomeController.