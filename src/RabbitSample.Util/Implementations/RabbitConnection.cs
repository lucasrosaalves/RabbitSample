using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.IO;
using System.Net.Sockets;

namespace RabbitSample.Util
{
    public class RabbitConnection : IRabbitConnection
    {
        private readonly ILogger<RabbitConnection> _logger;
        private readonly ConnectionFactory _connectionFactory;
        IConnection _connection;
        bool _disposed;
        object sync_root = new object();
        private readonly int _retryCount = 5;

        public RabbitConnection(ILogger<RabbitConnection> logger)
        {
            _logger = logger;
            _connectionFactory = new ConnectionFactory() { HostName = "localhost" };
            TryConnect();
        }


        public bool IsConnected
            => (_connection?.IsOpen ?? false) && !_disposed;

        public IModel CreateModel()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
            }

            return _connection.CreateModel();
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            try
            {
                _connection.Dispose();
            }
            catch (IOException ex)
            {
                _logger.LogCritical(ex.ToString());
            }
        }

        public bool TryConnect()
        {
            lock (sync_root)
            {
                var policy = Policy.Handle<SocketException>()
                    .Or<BrokerUnreachableException>()
                    .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                    {
                        _logger.LogWarning(ex, "RabbitMQ Client could not connect after {TimeOut}s ({ExceptionMessage})", $"{time.TotalSeconds:n1}", ex.Message);
                    }

                );

                policy.Execute(() =>
                {
                    _connection = _connectionFactory.CreateConnection();
                });

                if (!IsConnected)
                {
                    _logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");

                    return false;
                }

                _connection.ConnectionShutdown += OnConnectionShutdown;
                _connection.CallbackException += OnCallbackException;
                _connection.ConnectionBlocked += OnConnectionBlocked;

                _logger.LogInformation("RabbitMQ Client acquired a persistent connection to '{HostName}' and is subscribed to failure events", _connection.Endpoint.HostName);

                return true;
            }
        }

        private void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            HandleConnectionsProblems();
        }

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            HandleConnectionsProblems();
        }

        private void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
        {
            HandleConnectionsProblems();
        }

        private void HandleConnectionsProblems()
        {
            if (_disposed) return;

            TryConnect();
        }
    }
}