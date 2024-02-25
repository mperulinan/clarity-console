using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Server.Models;
using Portfolio.Server.Models.Others;

namespace Portfolio.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TradeController(PortfolioContext context, ILogger<TradeController> logger) : ControllerBase
    {
        [HttpGet]
        public Trade[] Get()
        {
            return [.. context.Trades];
        }

        [HttpGet("getAssetsWithHoldings")]
        public List<AssetWithHoldings> GetAssetsWithHoldings()
        {
            var holdings = new List<AssetWithHoldings>();
            foreach (var asset in GetAssets())
            {
                AssetWithHoldings assetWithHoldings = new()
                {
                    AssetId = asset,
                    Holdings = GetHoldingsByAsset(asset)
                };
                holdings.Add(assetWithHoldings);
            }
            return holdings;
        }

        private string[] GetAssets()
        {
            string[] fromAssets = [.. context.Trades.Select(t => t.FromAssetId).Distinct()];
            string[] toAssets = [.. context.Trades.Select(t => t.ToAssetId).Distinct()];
            return fromAssets.Concat(toAssets).Distinct().ToArray();
        }

        private decimal GetHoldingsByAsset(string asset)
        {
            return GetAmountReceivedByAsset(asset) - GetAmountSpentByAsset(asset);
        }

        private decimal GetAmountSpentByAsset(string asset)
        {
            return context.Trades.Where(t => t.FromAssetId == asset).Sum(t => t.AmountSpent);
        }

        private decimal GetAmountReceivedByAsset(string asset)
        {
            return context.Trades.Where(t => t.ToAssetId == asset).Sum(t => t.AmountReceived);
        }

        //[HttpGet("getAmountSpent")]



        [HttpPost]
        public ActionResult PostTrade(Trade trade)
        {
            context.Trades.Add(new Trade()
            {
                Date = DateTime.Now.ToUniversalTime(),
                FromAssetId = trade.FromAssetId,
                ToAssetId = trade.ToAssetId,
                AmountSpent = trade.AmountSpent,
                AmountReceived = trade.AmountReceived,
                Fee = trade.Fee,
                Notes = trade.Notes,
            });

            context.SaveChanges();

            return CreatedAtAction(nameof(PostTrade), new { id = trade.Id }, trade);
        }
    }
}
