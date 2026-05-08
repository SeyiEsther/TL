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
            return Ok(new
            {
                username = user.Username,
                displayName = user.DisplayName,
                isManager = user.IsManager,
            });
        }
    }
}