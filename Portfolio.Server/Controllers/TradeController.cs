using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Server.Models;

namespace Portfolio.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TradeController(PortfolioContext context) : ControllerBase
    {
        [HttpGet]
        public Trade[] Get()
        {
            return [.. context.Trades];
        }
    }
}
