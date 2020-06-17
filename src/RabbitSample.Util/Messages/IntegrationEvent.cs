using System;

namespace RabbitSample.Util
{
    public class IntegrationEvent
    {
        public IntegrationEvent(){}
        
        public IntegrationEvent(Guid id, DateTime date)
        {
            Id = id;
            Date = date;
            Description = $"Message {id} sent at {date}";
        }

        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
    }
}