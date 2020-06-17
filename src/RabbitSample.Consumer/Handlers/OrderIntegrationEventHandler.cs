using RabbitSample.Util;
using System.Threading.Tasks;

namespace RabbitSample.Consumer.Handlers
{
    public class OrderIntegrationEventHandler : IIntegrationEventHandler<OrderIntegrationEvent>
    {
        public Task Handle(OrderIntegrationEvent @event)
        {
            Task.Delay(5000);

            return Task.CompletedTask;
        }
    }
}
