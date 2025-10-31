using Microsoft.AspNetCore.Mvc;

namespace Waster.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {



        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "Healthy",
                timestamp = DateTime.UtcNow,
                message = "Waster API is running successfully!"
            });
        }

        [HttpGet("info")]
        public IActionResult GetInfo()
        {
            return Ok(new
            {
                status = "Healthy",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                machineName = Environment.MachineName,
                timestamp = DateTime.UtcNow,
                endpoints = new[]
                {
                    "/api/health",
                    "/api/health/info",
                    "/openapi/v1.json",
                    "/scalar/v1"
                }
            });
        }
    }
}