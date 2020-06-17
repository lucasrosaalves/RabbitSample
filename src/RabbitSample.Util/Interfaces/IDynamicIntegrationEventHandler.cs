using System.Threading.Tasks;

namespace RabbitSample.Util
{
    public interface IDynamicIntegrationEventHandler
    {
        Task Handle(dynamic eventData);
    }
}
