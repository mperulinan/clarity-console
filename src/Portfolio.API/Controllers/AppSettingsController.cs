using Microsoft.AspNetCore.Mvc;

namespace Portfolio.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppSettingsController(IConfiguration configuration) : ControllerBase
    {
        [HttpGet("{settingName}")]
        public ActionResult<string> Get(string settingName)
        {
            string? settingValue = configuration[settingName];
            if (string.IsNullOrWhiteSpace(settingValue))
            {
                return NotFound($"Setting '{settingName}' not found.");
            }

            return Ok(settingValue);
        }
    }
}
