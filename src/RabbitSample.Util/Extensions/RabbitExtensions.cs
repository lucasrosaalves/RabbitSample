using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace RabbitSample.Util.Extensions
{
    public static class RabbitExtensions
    {
        public static IServiceCollection AddRabbit(
            this IServiceCollection services,
            string hostName,
            string queueName = null)
        {

            services.AddSingleton<IEventBusSubscriptionsManager, EventBusSubscriptionsManager>();

            services.AddSingleton<IRabbitConnection, RabbitConnection>(sp =>
            {
                var factory = new ConnectionFactory()
                {
                    HostName = hostName,
                    DispatchConsumersAsync = true
                };

                var logger = sp.GetRequiredService<ILogger<RabbitConnection>>();

                return new RabbitConnection(factory, logger);
            });

            services.AddSingleton<IEventBus, EventBus>(sp =>
            {
                var rabbitConnection = sp.GetRequiredService<IRabbitConnection>();
                var subcriptionsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();
                var logger = sp.GetRequiredService<ILogger<EventBus>>();

                return new EventBus(rabbitConnection, subcriptionsManager, sp, logger, queueName);
            });

            return services;
        }
    }
}
