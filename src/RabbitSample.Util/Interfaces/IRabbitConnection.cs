using RabbitMQ.Client;

namespace RabbitSample.Util
{
    public interface IRabbitConnection
    {
        bool IsConnected {get; }
        IModel CreateModel();
        bool TryConnect();
        void Dispose();
    }
}