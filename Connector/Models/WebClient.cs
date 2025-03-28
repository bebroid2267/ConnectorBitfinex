using Newtonsoft.Json;
using System.Text.Json.Serialization;
using WebSocketSharp;

namespace Connector.Models
{
    public class SubscriptionModel
    {
        public string Event { get; set; }
        public string Channel { get; set; }
        public string Symbol { get; set; }
    }

    public class WebClient
    {
        private event Action<Trade> NewBuyTrade;
        private event Action<Trade> NewSellTrade;

        private WebSocket _webSocket;

        public WebClient()
        {
            _webSocket = new WebSocket(
                "wss://api-pub.bitfinex.com/ws/2");
        }

        private void SubscribeTrades(object? sender, MessageEventArgs args, string pair, int maxCount = 100)
        {
            SubscriptionModel model = new SubscriptionModel()
            {
                Event = "subscribe",
                Channel = "trades",
                Symbol = pair,
            };

            _webSocket.OnMessage += OnMessage;
            _webSocket.OnOpen += delegate
            {
                _webSocket.Send(JsonConvert.SerializeObject(model));
            };

            _webSocket.Connect();
        }
        private void UnsubscribeTrades(string pair)
        {

        }

        private event Action<Candle> CandleSeriesProcessing;
        private void SubscribeCandles(object? sender, MessageEventArgs args, string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0)
        {
            SubscriptionModel model = new SubscriptionModel()
            {
                Event = "subscribe",
                Channel = "candles",
                Symbol = pair,
            };

            _webSocket.OnMessage += OnMessage;
            _webSocket.OnOpen += delegate
            {
                _webSocket.Send(JsonConvert.SerializeObject(model));
            };

            _webSocket.Connect();

        }
        private void UnsubscribeCandles(string pair)
        {

        }
        private void OnMessage (object? sender, MessageEventArgs args)
        {

        }
    }
}
