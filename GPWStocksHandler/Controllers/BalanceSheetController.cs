using Microsoft.AspNetCore.Mvc;
using HtmlAgilityPack;
using System.Text;
using System.Text.Json;
using GPWStocksHandler.Services;

namespace GPWStocksHandler.Controllers
{
    [ApiController]
    [Route("api/balance-sheet")]
    public class BalanceSheetController : ControllerBase
    {
        private readonly IBalanceSheetService _balanceSheetService;

        public BalanceSheetController(IBalanceSheetService balanceSheetService)
        {
            this._balanceSheetService = balanceSheetService;
        }

        [HttpGet]
        [Route("current-assets/{ticker}")]
        public async Task<IActionResult> GetAssets([FromRoute]string ticker="CPS")
        {
            using var client = new HttpClient();

            string url = $"https://www.biznesradar.pl/raporty-finansowe-bilans/{ticker}"; 
            string html = await client.GetStringAsync(url);

            return Ok(this._balanceSheetService.GetBalanceSheetTable(html));
        }

        private string GetTable(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            HtmlNode table = doc.DocumentNode.SelectSingleNode("//table[contains(concat(' ', normalize-space(@class), ' '), ' report-table ')]");
            var tables = doc.DocumentNode.SelectNodes("//table");

            StringBuilder data=new StringBuilder();
            if (table != null)
            {
                var years = table.SelectNodes(".//th[contains(@class, 'thq h')]");
                var row = table.SelectSingleNode(".//tr[@data-field='BalanceCurrentAssets']");

                var span = row.SelectNodes(".//span[contains(@class, 'value')]").Select(s=>s.InnerText.Trim());
               
                var newYears = new List<string>();
                foreach(var y in years)
                {
                    string directText = string.Concat(
                            y.ChildNodes
                                .Where(n => n.NodeType == HtmlNodeType.Text)
                                .Select(n => n.InnerText)
                        ).Trim();

                    newYears.Add(directText);
                }


                var items = span.Zip(newYears, (price, year) => new { price,year});

                return JsonSerializer.Serialize(items);   
            }

            return "Table null";
        }
    }
}
