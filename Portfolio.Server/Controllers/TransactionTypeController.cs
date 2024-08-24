using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Server.Models;

namespace Portfolio.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionTypeController(PortfolioContext context) : ControllerBase
    {
        [HttpGet]
        public TransactionType[] Get()
        {
            return [.. context.TransactionTypes];
        }
    }
}
