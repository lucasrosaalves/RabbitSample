using RabbitMQ.Client;

namespace RabbitSample.Util
{
    public class RabbitConnection : IRabbitConnection
    {
        public RabbitConnection()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            
            Connection = factory.CreateConnection();              
        }
        public IConnection Connection { get; }
        
    }
}