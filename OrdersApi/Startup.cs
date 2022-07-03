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
            // Register the DbContext.
            services.AddDbContext<OrdersContext>(options => options.UseSqlServer(
                Configuration.GetConnectionString("OrdersConnection") // connection string from appSettings.json.
            ));

            // Dependency injection for OrderRepository
            services.AddTransient<IOrderRepository, OrderRepository>();

            services
                .AddControllers()
                .AddDapr(); // Add Dapr capabilities.
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
        }
    }
}
