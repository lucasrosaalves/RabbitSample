using Microsoft.AspNetCore.Mvc;
using RabbitSample.Util;
using System;

namespace RabbitSample.Producer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProducerController : ControllerBase
    {

        private readonly IEventBus _eventBus;

        public ProducerController(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        [HttpGet("")]
        public IActionResult Get()
        {
            return Ok("Producer up"); 
        }

        [HttpGet("sendmessage")]
        public IActionResult SendMessage()
        {
            try
            {
                var @event = new OrderIntegrationEvent(Guid.NewGuid(), DateTime.Now);

                _eventBus.Publish(@event);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
         
        }
    }
}