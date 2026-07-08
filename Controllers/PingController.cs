using Microsoft.AspNetCore.Mvc;

namespace TL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PingController : ControllerBase
    {
        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public IActionResult Get()
        {
            Response.Headers.CacheControl = "no-store, no-cache";
            return NoContent();
        }
    }
}
