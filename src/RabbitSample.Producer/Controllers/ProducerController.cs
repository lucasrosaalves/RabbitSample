using Microsoft.AspNetCore.Mvc;

namespace RabbitSample.Producer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProducerController : ControllerBase
    {

        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Producer up"); 
        }
    }
}