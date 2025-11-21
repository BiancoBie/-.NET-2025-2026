using Microsoft.AspNetCore.Mvc;

namespace OrderManagementAPI.Controllers
{
    //controller for health check
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        //health status and feature list
        [HttpGet]
        public ActionResult<object> GetHealth()
        {
            return Ok(new { 
                Status = "Healthy", 
                Timestamp = DateTime.UtcNow,
                Version = "1.0.0",
                Features = new[]
                {
                    "Advanced AutoMapper Patterns",
                    "Structured Logging & Telemetry", 
                    "Complex Order Validation",
                    "Correlation ID Tracking",
                    "Performance Monitoring",
                    "Business Rules Validation",
                    "MVC Architecture"
                }
            });
        }
    }
}