using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitSample.Util;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitSample.Consumer.Services
{
    public class ConsumerService : BackgroundService
    {
        private readonly string _queue = "rabbitmqsample";
        private readonly IModel _channel;
        private readonly ILogger<ConsumerService> _logger;

        public ConsumerService(
            IRabbitConnection rabbitConnection,
            ILogger<ConsumerService> logger)
        {

            _channel = rabbitConnection.CreateModel();
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _channel.QueueDeclare(_queue,
                true,
                false,
                false,
                null);

            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();

                _logger.LogInformation($"Message received: {Encoding.UTF8.GetString(body)}");
                var message = JsonSerializer.Deserialize<IntegrationEvent>(body);


                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);

            };

            _channel.BasicConsume(
                 _queue,
                 false,
                 consumer);

            return Task.CompletedTask;
        }
    }
}