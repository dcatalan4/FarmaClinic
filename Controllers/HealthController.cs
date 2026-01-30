using Microsoft.AspNetCore.Mvc;

namespace ControlInventario.Controllers
{
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                service = "FarmaClinic API"
            });
        }
    }
}
