using Microsoft.AspNetCore.Mvc;

namespace RabbitSample.Consumer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConsumerController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Consumer up");
        }
    }
}