using System;
using Microsoft.AspNetCore.Mvc;
using RabbitSample.Producer.Services;

namespace RabbitSample.Producer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProducerController : ControllerBase
    {
        private readonly IProducerService _producerService;

        public ProducerController(IProducerService producerService)
        {
            _producerService = producerService;
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
                _producerService.SendMessage();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
         
        }
    }
}