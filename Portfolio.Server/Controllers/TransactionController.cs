using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.MSIdentity.Shared;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.ContentModel;
using Portfolio.Server.Models;
using Portfolio.Server.Models.Requests;
using RestSharp;

namespace Portfolio.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController(PortfolioContext context, ILogger<TransactionController> logger) : ControllerBase
    {
        [HttpGet]
        public Transaction[] Get()
        {
            return [.. context.Transactions];
        }


        [HttpPost]
        public ActionResult PostTransaction(NewTransactionRequest request)
        {
            Transaction newTransaction = new()
            {
                Date = request.Date.ToUniversalTime(),
                TransactionType = request.TransactionType,
                FromAssetId = request.FromAssetId,
                ToAssetId = request.ToAssetId,
                AmountSpent = request.AmountSpent,
                AmountReceived = request.AmountReceived,
                FromAssetPriceInEur = request.FromAssetPriceInEur,
                Fee = request.Fee,
                FeeAsset = request.FeeAsset,
                FeeAssetPriceInEur = request.FeeAssetPriceInEur,
                Notes = request.Notes,
            };
            context.Transactions.Add(newTransaction);

            context.SaveChanges();

            return CreatedAtAction(nameof(PostTransaction), new { id = newTransaction.Id }, newTransaction);
        }


        [HttpPost("postPricesInEur")]
        public async Task PostPricesInEurBinance()
        {
            try
            {
                List<Transaction> transactions = [.. context.Transactions.Where(t => t.FeeAsset == "bitcoin")];
                foreach (var transaction in transactions)
                {
                    DateTimeOffset dateTimeOffset = new(transaction.Date, TimeSpan.Zero);
                    long unixTimeMilliseconds = dateTimeOffset.ToUnixTimeMilliseconds();

                    // Binance API parameter configuration
                    string symbol = "BTCEUR";
                    string interval = "1s"; // 1 second interval
                    long startTime = unixTimeMilliseconds - 1000; // 1 second before
                    long endTime = unixTimeMilliseconds + 1000; // 1 second after

                    var options = new RestClientOptions($"https://api.binance.com/api/v3/klines?symbol={symbol}&interval={interval}&startTime={startTime}&endTime={endTime}&limit=1");
                    var client = new RestClient(options);
                    var request = new RestRequest("", Method.Get);
                    request.AddHeader("accept", "application/json");
                    var response = await client.ExecuteAsync(request);

                    if (!response.IsSuccessful)
                    {
                        continue;
                    }

                    if (response.Content == null)
                    {
                        continue;
                    }

                    JArray klines = JArray.Parse(response.Content);
                    if (klines.Count == 0)
                    {
                        continue;
                    }

                    // Obtener los datos de la vela
                    var kline = klines[0];
                    var highPriceToken = kline[2];
                    if (highPriceToken == null)
                    {
                        continue;
                    }

                    decimal highPrice = (decimal)highPriceToken;
                    //transaction.FromAssetPriceInEur = 1/highPrice;      //  From
                    transaction.FeeAssetPriceInEur = highPrice;       //  Fee
                    context.SaveChanges();
                }
            }
            catch (Exception)
            {

            }
        }
    }
}
