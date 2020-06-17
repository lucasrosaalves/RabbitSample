using System;

namespace RabbitSample.Util
{
    public class OrderIntegrationEvent : IntegrationEvent
    {
        public OrderIntegrationEvent()
        {
        }
        public OrderIntegrationEvent(Guid id, DateTime date)
            : base(id, date)
        {

        }
    }
}
