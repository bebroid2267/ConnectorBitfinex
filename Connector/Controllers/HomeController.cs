using Connector.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Connector.Controllers
{
    [Route("api/trades")]
    [ApiController]
    public class HomeController : Controller
    {
        private readonly ConnectorBitfinex _connector = new ConnectorBitfinex();
        private readonly IHubContext<TradeHub> _hubContext;
        public HomeController(IHubContext<TradeHub> hubContext)
        {
            _hubContext = hubContext;

            _connector.NewSellTrade += async (trade) => await _hubContext.Clients.All.SendAsync("ReceiveSellTrade", trade);
            _connector.NewBuyTrade += async (trade) => await _hubContext.Clients.All.SendAsync("ReceiveBuyTrade", trade);
            _connector.CandleSeriesProcessing += async (candle) => await _hubContext.Clients.All.SendAsync("ReceiveCandle", candle);

            _ = Task.Run(async () => await _connector.ListenSocket());
        }
        [HttpPost("subscribeTrade/{pair}")]
        public IActionResult SubscribeTrade(string pair)
        {
            _connector.SubscribeTrades(pair);
            return Ok("Подписка на обновления трейдов запущена.");
        }
        [HttpPost("subscribeCandle/{pair}")]

        public IActionResult SubscribeCandle(string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0)
        {
            _connector.SubscribeCandles(pair, periodInSec, from, to, count);
            return Ok("Подписка на обновления свечей запущена.");
        }
        [HttpPost("unsubscribeTrade/{pair}")]

        public IActionResult UnSubscribeTrade(string pair)
        {
            _connector.UnsubscribeTrades(pair);
            return Ok("Отписка на обновления трейдов.");
        }

        [HttpPost("unsubscribeCandle/{pair}")]
        public IActionResult UnSubscribeCandle(string pair)
        {
            _connector.UnsubscribeCandles(pair);
            return Ok("Отписка на обновления свечей");
        }


        [HttpGet("candles/{pair}")]
        public async Task<IActionResult> GetCandles(string pair, string timeFrame = "1m", int period = 30, long count = 100)
        {
            var candles = await _connector.GetCandleSeriesAsync(pair, period, null, null, count);
            return Ok(candles);
        }

        [HttpGet("trades/{pair}")]
        public async Task<IActionResult> GetTrades(string pair, int maxCount)
        {
            var trades = await _connector.GetNewTradesAsync(pair, maxCount);
            return Ok(trades);
        }

    }
}
