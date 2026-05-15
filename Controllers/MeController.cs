using Microsoft.AspNetCore.Mvc;
using TL.Services;

namespace TL.Controllers
{
    [ApiController]
    [Route("api/me")]
    public class MeController : ControllerBase
    {
        private readonly UserService _users;
        public MeController(UserService users) => _users = users;

        [HttpGet]
        public IActionResult Get()
        {
            var user = _users.GetCurrentUser();
            var rawIdentity = HttpContext.User?.Identity?.Name;
            var envUser = Environment.UserName;
            return Ok(new
            {
                username = user.Username,
                displayName = user.DisplayName,
                isManager = user.IsManager,
                debug_rawIdentity = rawIdentity,
                debug_envUserName = envUser,
            });
        }
    }
}