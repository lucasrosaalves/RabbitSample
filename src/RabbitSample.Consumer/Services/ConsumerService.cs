using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitSample.Util;

namespace RabbitSample.Consumer.Services
{
    public class ConsumerService : BackgroundService
    {
        private readonly string _queue = "rabbitmqsample";
        private readonly IRabbitConnection _rabbitConnection;
        private readonly ILogger<ConsumerService> _logger;

        public ConsumerService(
            IRabbitConnection rabbitConnection,
            ILogger<ConsumerService> logger)
        {
            _rabbitConnection = rabbitConnection;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var channel = _rabbitConnection.Connection.CreateModel())
            {
                channel.QueueDeclare(_queue,
                    false,
                    false,
                    false,
                    null);

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();

                    _logger.LogInformation($"Message received: {Encoding.UTF8.GetString(body)}");
                    // var message = JsonSerializer.Deserialize<Message>(body);
                };
                
                channel.BasicConsume(
                    _queue,
                    true,
                    consumer);
            }

            return Task.CompletedTask;
        }
    }
}