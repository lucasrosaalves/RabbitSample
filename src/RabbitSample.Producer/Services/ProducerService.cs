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
            using(var channel = _rabbitConnection.Connection.CreateModel())
            {
                var message = new Message(Guid.NewGuid(), DateTime.Now);
                
                channel.QueueDeclare(queue: _queue,
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);
                
                var body = JsonSerializer.SerializeToUtf8Bytes(message);

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