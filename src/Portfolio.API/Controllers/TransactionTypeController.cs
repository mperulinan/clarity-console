using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.API.Controllers
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
