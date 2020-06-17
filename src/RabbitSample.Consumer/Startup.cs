using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitSample.Util;

namespace RabbitSample.Consumer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public IConfiguration Configuration { get; }
        
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            services.AddSingleton<IEventBusSubscriptionsManager, EventBusSubscriptionsManager>();

            services.AddSingleton<IRabbitConnection, RabbitConnection>(sp =>
            {
                var factory = new ConnectionFactory()
                {
                    HostName = "localhost",
                    DispatchConsumersAsync = true
                };

                var logger = sp.GetRequiredService<ILogger<RabbitConnection>>();

                return new RabbitConnection(factory, logger, 5);
            });

            services.AddSingleton<IEventBus, EventBus>(sp =>
            {
                var rabbitConnection = sp.GetRequiredService<IRabbitConnection>();
                var subcriptionsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();
                var logger = sp.GetRequiredService<ILogger<EventBus>>();

                return new EventBus(rabbitConnection, subcriptionsManager, sp, logger, "consumer", 5);
            });

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });

            ConfigureEventBus(app);
        }

        private void ConfigureEventBus(IApplicationBuilder app)
        {
            var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();

            eventBus.Subscribe<OrderIntegrationEvent, IIntegrationEventHandler<OrderIntegrationEvent>>();

        }
    }
}