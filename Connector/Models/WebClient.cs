using Newtonsoft.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using WebSocketSharp;

namespace Connector.Models
{
    public class SubscriptionModel
    {
        public string Event { get; set; }
        public string Channel { get; set; }
        public string Symbol { get; set; }
        public string Key { get; set; }

    }

    public class WebClient
    {
        public event Action<Trade> NewBuyTrade;
        public event Action<Trade> NewSellTrade;

        public event Action<Candle> CandleSeriesProcessing;

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

            _webSocket.OnMessage += OnTradeUpdate;
            _webSocket.OnOpen += delegate
            {
                _webSocket.Send(JsonConvert.SerializeObject(model));
            };

            _webSocket.Connect();
        }
        private void UnsubscribeTrades(string pair)
        {
            SubscriptionModel model = new SubscriptionModel()
            {
                Event = "unsubscribe",
                Channel = "trades",
                Symbol = pair,
            };

            _webSocket.Send(JsonConvert.SerializeObject(model));
           
            _webSocket.Close();
        }

        private void SubscribeCandles(object? sender, MessageEventArgs args, string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0)
        {
            SubscriptionModel model = new SubscriptionModel()
            {
                Event = "subscribe",
                Channel = "candles",
                Key = $"trade:1m:{pair}:a30:{from}:{to}",
            };

            _webSocket.OnMessage += OnCandleUpdate;
            _webSocket.OnOpen += delegate
            {
                _webSocket.Send(JsonConvert.SerializeObject(model));
            };

            _webSocket.Connect();

        }
        private void UnsubscribeCandles(string pair)
        {
            SubscriptionModel model = new SubscriptionModel()
            {
                Event = "unsubscribe",
                Channel = "candles",
                Key = pair,
            };

            _webSocket.Send(JsonConvert.SerializeObject(model));

            _webSocket.Close();
        }

        private void OnTradeUpdate (object? sender, MessageEventArgs args)
        {
            ParseTradeData(args.Data);
        }

        private void OnCandleUpdate(object? sender, MessageEventArgs args)
        {
           ParseCandleData(args.Data);
        }

        private void ProcessTrade(JsonElement trade)
        {
            var parsedTrade = new Trade
            {
                Id = trade[0].GetInt32().ToString(),
                Time = DateTimeOffset.FromUnixTimeMilliseconds(trade[1].GetInt64()),
                Amount = trade[2].GetDecimal(),
                Price = trade[3].GetDecimal(),
                Side = trade[2].GetDecimal() > 0 ? "buy" : "sell",
                Pair = "BTC/USD"
            };

            if (parsedTrade.Side == "buy")
            {
                NewBuyTrade(parsedTrade);
            }
            else
            {
                NewSellTrade(parsedTrade);
            }

            //Console.WriteLine($"[TRADE] {parsedTrade.Time}: {parsedTrade.Side} {parsedTrade.Amount} BTC @ {parsedTrade.Price} USD");
        }

        private void ParseCandleData(string json)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 1)
                {
                    int channelId = root[0].GetInt32();
                    JsonElement data = root[1];

                    if (data.ValueKind == JsonValueKind.Array)
                    {
                        if (data.GetArrayLength() > 1 && data[0].ValueKind == JsonValueKind.Array)
                        {
                            // Snapshot (много свечей)
                            foreach (JsonElement candle in data.EnumerateArray())
                            {
                                ProcessCandle(candle);
                            }
                        }
                        else
                        {
                            // Update (одна свеча)
                            ProcessCandle(data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка парсинга свечей: {ex.Message}");
            }
        }

        private void ProcessCandle(JsonElement candle)
        {
            var parsedCandle = new Candle
            {
                OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(candle[0].GetInt64()),
                OpenPrice = candle[1].GetDecimal(),
                ClosePrice = candle[2].GetDecimal(),
                HighPrice = candle[3].GetDecimal(),
                LowPrice = candle[4].GetDecimal(),
                TotalVolume = candle[5].GetDecimal(),
                Pair = "BTC/USD"
            };

            CandleSeriesProcessing.Invoke(parsedCandle);
            //Console.WriteLine($"[CANDLE] {parsedCandle.OpenTime}: O:{parsedCandle.OpenPrice}, C:{parsedCandle.ClosePrice}, H:{parsedCandle.HighPrice}, L:{parsedCandle.LowPrice}, V:{parsedCandle.TotalVolume}");
        }

        private void ParseTradeData(string json)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 1)
                {
                    int channelId = root[0].GetInt32();
                    JsonElement data = root[1];

                    if (data.ValueKind == JsonValueKind.Array)
                    {
                        if (data.GetArrayLength() > 1 && data[0].ValueKind == JsonValueKind.Array)
                        {
                            // Snapshot (много трейдов)
                            foreach (JsonElement trade in data.EnumerateArray())
                            {
                                ProcessTrade(trade);
                            }
                        }
                        else
                        {
                            // Update (один трейд)
                            ProcessTrade(data);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка парсинга трейдов: {ex.Message}");
            }
        }
    }
}
