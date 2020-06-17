using System;

namespace RabbitSample.Util
{
    public class Message
    {
        protected Message(){}
        
        public Message(Guid id, DateTime date)
        {
            Id = id;
            Date = date;
            Description = $"Message {id} sent at {date}";
        }

        public Guid Id { get; }
        public DateTime Date { get;  }
        public string Description { get; }
    }
}