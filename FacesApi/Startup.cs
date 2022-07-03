using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace FacesApi
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
            // If using Kestrel: enable SixLabors to synchronously process images.
            services.Configure<KestrelServerOptions>(options => { options.AllowSynchronousIO = true; });
            //// If using IIS: enable SixLabors to synchronously process images.
            //services.Configure<IISServerOptions>(options => { options.AllowSynchronousIO = true; });

            // Bind Azure Face app settings (appSettings.json) through dependency injection (singleton).
            var config = new AzureFaceConfiguration();
            Configuration.Bind("AzureFaceCredentials", config);
            services.AddSingleton(config);

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
