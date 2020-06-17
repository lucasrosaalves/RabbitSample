using System;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitSample.Util;

namespace RabbitSample.Producer.Services
{
    public class ProducerService : IProducerService
    {
        private readonly string _queue = "rabbitmqsample";
        
        private readonly IRabbitConnection _rabbitConnection;
        private readonly ILogger<ProducerService> _logger;

        public ProducerService(IRabbitConnection rabbitConnection,
            ILogger<ProducerService> logger)
        {
            _rabbitConnection = rabbitConnection;
            _logger = logger;
        }
        
        public void SendMessage()
        {
            using(var channel = _rabbitConnection.CreateModel())
            {
                var message = new IntegrationEvent(Guid.NewGuid(), DateTime.Now);
                
                channel.QueueDeclare(queue: _queue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;

                channel.BasicPublish(
                    exchange: "",
                    routingKey: _queue,
                    basicProperties: null,
                    body: body);
                
                _logger.LogInformation($"Message sent at {DateTime.Now}");
            }
        }
    }


}