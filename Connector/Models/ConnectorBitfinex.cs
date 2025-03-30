namespace Connector.Models
{
    public class ConnectorBitfinex: ITestConnector
    {
        private readonly RestClient _restConnector = new RestClient();

        private readonly WebSocketClient _webSocketConnector = new WebSocketClient();

        public event Action<Candle> CandleSeriesProcessing;
        public event Action<Trade> NewBuyTrade;
        public event Action<Trade> NewSellTrade;


        public ConnectorBitfinex()
        {
            _webSocketConnector.NewBuyTrade += delegate (Trade trade)
            {
                NewBuyTrade.Invoke(trade);
            };
            _webSocketConnector.NewSellTrade += delegate (Trade trade)
            {
                NewSellTrade.Invoke(trade);
            };
            _webSocketConnector.CandleSeriesProcessing += delegate (Candle candle)
            {
                CandleSeriesProcessing.Invoke(candle);
            };
            _webSocketConnector.ConnectAsync();
        }

        public async Task<IEnumerable<Trade>>  GetNewTradesAsync(string pair, int maxCount)
        {
            return await _restConnector.GetTrades(pair, maxCount);
        }
        public async Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0)
        {
            return await _restConnector.GetCandles(pair, "1m", periodInSec, from, to, count);
        }

        public void SubscribeTrades(string pair, int maxCount = 100)
        {
             _webSocketConnector.SubscribeTrades(pair, maxCount);
        }
        public void UnsubscribeTrades(string pair)
        {
             _webSocketConnector.UnsubscribeTrades(pair);
        }

        public void SubscribeCandles(string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0)
        {
             _webSocketConnector.SubscribeCandles(pair, periodInSec, from, to, count);
        }

        public void  UnsubscribeCandles(string pair)
        {
             _webSocketConnector.UnsubscribeCandles(pair);
        }
        public async Task ListenSocket()
        {
            await _webSocketConnector.ListenAsync();
        }

    }
}
