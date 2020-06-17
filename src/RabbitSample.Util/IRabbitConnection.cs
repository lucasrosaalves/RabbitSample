using RabbitMQ.Client;

namespace RabbitSample.Util
{
    public interface IRabbitConnection
    {
        IConnection Connection { get; }
    }
}