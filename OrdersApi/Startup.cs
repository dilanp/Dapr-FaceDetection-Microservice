using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using OrdersApi.Persistence;

namespace OrdersApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.
                AddSingleton<IConfig>(Configuration.GetSection("CustomConfig")?.Get<Config>());

            //// Register the DbContext.
            //services.AddDbContext<OrdersContext>(options => options.UseSqlServer(
            //    Configuration.GetConnectionString("OrdersConnection") // connection string from appSettings.json.
            //));

            // If Tye orchestration is not used then comment this and uncomment the above DB config.
            AddDbContexts(services);

            // Dependency injection for OrderRepository
            services.AddTransient<IOrderRepository, OrderRepository>();

            services
                .AddControllers()
                .AddDapr(); // Add Dapr capabilities.
        }

        public void AddDbContexts(IServiceCollection services)
        {

            services.AddDbContext<OrdersContext>(opt =>
            {
                var connectionString = Configuration.GetConnectionString("sql-order") ?? // Name in Tye.yaml.
                                       "name=OrdersConnection";
                Console.Write("ConString:" + connectionString + " ");
                opt.UseSqlServer(connectionString, opt => opt.EnableRetryOnFailure(5));
            }, ServiceLifetime.Transient);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCloudEvents(); // Enable middleware pipeline to respond to cloud event payloads.
            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapSubscribeHandler(); // Maps an endpoint that will respond to requests to '/dapr/subscribe' from Dapr runtime.
            });

            TryRunMigrations(app);
        }

        private static void TryRunMigrations(IApplicationBuilder app)
        {
            var config = app.ApplicationServices.GetService<IConfig>();
            if (config?.RunDbMigrations == true)
            {
                using (var scope = app.ApplicationServices.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<OrdersContext>();
                    dbContext.Database.Migrate();
                }
            }
        }
    }
}
