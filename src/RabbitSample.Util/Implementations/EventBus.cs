using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RabbitSample.Util
{
    public class EventBus : IEventBus, IDisposable
    {
        const string BROKER_NAME = "rabbitsample_event_bus";

        private readonly IRabbitConnection _rabbitConnection;
        private readonly IEventBusSubscriptionsManager _subsManager;
        private readonly ILogger<EventBus> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly int _retryCount;
        private IModel _consumerChannel;
        private string _queueName;

        public EventBus(
            IRabbitConnection rabbitConnection,
            IEventBusSubscriptionsManager subsManager,
            IServiceProvider serviceProvider,
            ILogger<EventBus> logger,
            string queueName = null,
            int retryCount = 5)
        {
            _rabbitConnection = rabbitConnection;
            _logger = logger;
            _subsManager = subsManager;
            _serviceProvider = serviceProvider;
            _queueName = queueName;
            _consumerChannel = CreateConsumerChannel();
            _retryCount = retryCount;
            _subsManager.OnEventRemoved += SubsManager_OnEventRemoved;
        }

        private void SubsManager_OnEventRemoved(object sender, string eventName)
        {
            if (!_rabbitConnection.IsConnected)
            {
                _rabbitConnection.TryConnect();
            }

            using (var channel = _rabbitConnection.CreateModel())
            {
                channel.QueueUnbind(queue: _queueName,
                    exchange: BROKER_NAME,
                    routingKey: eventName);

                if (_subsManager.IsEmpty)
                {
                    _queueName = string.Empty;
                    _consumerChannel.Close();
                }
            }
        }

        public void Publish(IntegrationEvent @event)
        {
            if (!_rabbitConnection.IsConnected)
            {
                _rabbitConnection.TryConnect();
            }

            var policy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogWarning(ex, "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})", @event.Id, $"{time.TotalSeconds:n1}", ex.Message);
                });

            var eventName = @event.GetType().Name;

            using (var channel = _rabbitConnection.CreateModel())
            {

                channel.ExchangeDeclare(exchange: BROKER_NAME, type: "direct");

                var message = JsonSerializer.Serialize(@event);

                var body = Encoding.UTF8.GetBytes(message);

                policy.Execute(() =>
                {
                    var properties = channel.CreateBasicProperties();
                    properties.DeliveryMode = 2; // persistent

                    channel.BasicPublish(
                        exchange: BROKER_NAME,
                        routingKey: eventName,
                        mandatory: true,
                        basicProperties: properties,
                        body: body);
                });
            }
        }

        public void SubscribeDynamic<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            DoInternalSubscription(eventName);

            _subsManager.AddDynamicSubscription<TH>(eventName);

            StartBasicConsume();
        }

        public void Subscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            var eventName = _subsManager.GetEventKey<T>();
            DoInternalSubscription(eventName);

            _subsManager.AddSubscription<T, TH>();
            StartBasicConsume();
        }

        private void DoInternalSubscription(string eventName)
        {
            var containsKey = _subsManager.HasSubscriptionsForEvent(eventName);
            if (!containsKey)
            {
                if (!_rabbitConnection.IsConnected)
                {
                    _rabbitConnection.TryConnect();
                }

                using (var channel = _rabbitConnection.CreateModel())
                {
                    channel.QueueBind(queue: _queueName,
                                      exchange: BROKER_NAME,
                                      routingKey: eventName);
                }
            }
        }

        public void Unsubscribe<T, TH>()
            where T : IntegrationEvent
            where TH : IIntegrationEventHandler<T>
        {
            _subsManager.RemoveSubscription<T, TH>();
        }

        public void UnsubscribeDynamic<TH>(string eventName)
            where TH : IDynamicIntegrationEventHandler
        {
            _subsManager.RemoveDynamicSubscription<TH>(eventName);
        }

        public void Dispose()
        {
            if(_consumerChannel is null)
            {
                _subsManager.Clear();
                return;
            }

            _consumerChannel.Dispose();
        }

        private void StartBasicConsume()
        {
            if (_consumerChannel is null)
            {
                return;
            }

            var consumer = new AsyncEventingBasicConsumer(_consumerChannel);

            consumer.Received += Consumer_Received;

            _consumerChannel.BasicConsume(queue: _queueName,
                autoAck: false,
                consumer: consumer);

        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
        {
            var eventName = eventArgs.RoutingKey;

            var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

            try
            {
                await ProcessEvent(eventName, message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ERROR Processing message \"{Message}\"", message);
            }

            _consumerChannel.BasicAck(eventArgs.DeliveryTag, multiple: false);
        }

        private IModel CreateConsumerChannel()
        {
            if (!_rabbitConnection.IsConnected)
            {
                _rabbitConnection.TryConnect();
            }

            var channel = _rabbitConnection.CreateModel();

            channel.ExchangeDeclare(exchange: BROKER_NAME, type: "direct");

            channel.QueueDeclare(queue: _queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            channel.CallbackException += (sender, ea) =>
            {
                _consumerChannel.Dispose();
                _consumerChannel = CreateConsumerChannel();
                StartBasicConsume();
            };

            return channel;
        }

        private async Task ProcessEvent(string eventName, string message)
        {
            if (!_subsManager.HasSubscriptionsForEvent(eventName))
            {
                _logger.LogWarning("No subscription for RabbitMQ event: {EventName}", eventName);
                return;
            }

            var subscriptions = _subsManager.GetHandlersForEvent(eventName) ?? new List<SubscriptionInfo>();

            if (!subscriptions.Any()) { return; }

            using (var scope = _serviceProvider.CreateScope())
            {
                foreach (var subscription in subscriptions)
                {
                    var handler = scope.ServiceProvider.GetRequiredService(subscription.HandlerType);

                    if (handler is null)
                    {
                        continue;
                    }

                    await ProcessSubscription(handler, eventName, message);
                }
            }
        }

        private async Task ProcessSubscription(object handler, string eventName, string message)
        {
            var eventType = _subsManager.GetEventTypeByName(eventName);

            var integrationEvent = JsonSerializer.Deserialize(message, eventType);

            var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);

            await Task.Yield();
            await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { integrationEvent });
        }
    }
}
