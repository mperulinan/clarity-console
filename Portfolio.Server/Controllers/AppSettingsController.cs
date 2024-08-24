using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Server.Models;
using System.Configuration;

namespace Portfolio.Server.Controllers
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
