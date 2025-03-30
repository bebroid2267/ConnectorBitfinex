using Newtonsoft.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json.Linq;


namespace Connector.Models
{
    public class SubscriptionModel
    {
        public string Event { get; set; }
        public string Channel { get; set; }
        public string Symbol { get; set; }
        public string Key { get; set; }

    }

    public class WebSocketClient
    {
        public event Action<Trade> NewBuyTrade;
        public event Action<Trade> NewSellTrade;

        public event Action<Candle> CandleSeriesProcessing;

        private ClientWebSocket _webSocket = new ClientWebSocket();
        private readonly Uri _uri = new Uri("wss://api-pub.bitfinex.com/ws/2");

        private readonly Dictionary<int, string> _subscriptions = new Dictionary<int, string>();


        public async Task ConnectAsync()
        {
            await _webSocket.ConnectAsync(_uri, CancellationToken.None);
        }

        public async Task SubscribeTrades(string pair, int maxCount = 100)
        {
            while (_webSocket.State == WebSocketState.Connecting)
            {
                Console.WriteLine("⏳ Ожидание подключения...");
                await Task.Delay(100); // Ожидаем подключение
            }

            if (_webSocket.State != WebSocketState.Open)
            {
                Console.WriteLine($"❌ WebSocket не подключился, текущий статус: {_webSocket.State}");
                return;
            }


            var model = new
            {
                @event = "subscribe",
                channel = "trades",
                symbol = pair
            };

            string message = JsonConvert.SerializeObject(model);
            byte[] bytes = Encoding.UTF8.GetBytes(message);

            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);

        }
        public async Task ListenAsync()
        {
            while (_webSocket.State == WebSocketState.Connecting)
            {
                Console.WriteLine("⏳ Ожидание подключения...");
                await Task.Delay(100); // Ожидаем подключение
            }

            if (_webSocket.State != WebSocketState.Open)
            {
                Console.WriteLine($"❌ WebSocket не подключился, текущий статус: {_webSocket.State}");
                return;
            }

            byte[] buffer = new byte[8192];

            while (_webSocket.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                ProcessMessage(message);
            }
        }
        private void ProcessMessage(string message)
        {
            try
            {
                // Парсим сообщение
                JToken data = JToken.Parse(message);

                // 1. Если это объект (JObject), значит, это информация о подписке
                if (data is JObject obj && obj["event"] != null && obj["event"].ToString() == "subscribed")
                {
                    int chanId = obj["chanId"].ToObject<int>();
                    string channel = obj["channel"].ToString();
                    _subscriptions[chanId] = channel;

                    Console.WriteLine($"✅ Подписка подтверждена: {channel} (chanId: {chanId})");
                    return;
                }

                // 2. Если это массив (JArray), значит, это данные от подписки
                if (data is JArray array && array.Count > 1)
                {
                    int chanId = array[0].ToObject<int>();

                    if (_subscriptions.ContainsKey(chanId))
                    {
                        string channel = _subscriptions[chanId];

                        if (channel == "trades")
                        {
                            OnTradeUpdate(array.ToString());
                        }
                        else if (channel == "candles")
                        {
                            OnCandleUpdate(array.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка обработки сообщения: {ex.Message}");
            }
        }


        public async Task UnsubscribeTrades(string pair)
        {
            var model = new
            {
                @event = "unsubscribe",
                channel = "trades",
                symbol = pair
            };

            string message = JsonConvert.SerializeObject(model);
            byte[] bytes = Encoding.UTF8.GetBytes(message);

            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async Task SubscribeCandles( string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0)
        {
            var model = new
            {
                @event = "subscribe",
                channel = "candles",
                key = $"trade:1m:{pair}:a30:{from}:{to}",
            };

            string message = JsonConvert.SerializeObject(model);
            byte[] bytes = Encoding.UTF8.GetBytes(message);

            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        public async Task UnsubscribeCandles(string pair)
        {
            var model = new
            {
                @event = "unsubscribe",
                channel = "candles",
                key = pair
            };

            string message = JsonConvert.SerializeObject(model);
            byte[] bytes = Encoding.UTF8.GetBytes(message);

            await _webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private void OnTradeUpdate (string message)
        {
             var result = ParseTradeData(message);
            if (result.Side == "buy")
            {
                NewBuyTrade.Invoke(result);
            }
            else
            {
                NewSellTrade.Invoke(result);
            }
        }

        private void OnCandleUpdate(string message)
        {
           CandleSeriesProcessing.Invoke(ParseCandleData(message));
        }

        private Trade ProcessTrade(JsonElement trade)
        {
            var parsedTrade = new Trade
            {
                Id = trade[0].GetInt32().ToString(),
                Time = DateTimeOffset.FromUnixTimeMilliseconds(trade[1].GetInt64()),
                Amount = trade[2].GetDecimal(),
                Price = trade[3].GetDecimal(),
                Pair = "BTC/USD"
            };

            return parsedTrade;
        }

        private Candle ParseCandleData(string json)
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
                            return ProcessCandle(data.EnumerateArray().First());
                                // Snapshot (много свечей)
                        }
                        else
                        {
                            // Update (одна свеча)
                            return ProcessCandle(data);
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private Candle ProcessCandle(JsonElement candle)
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

            return parsedCandle;
        }

        private Trade ParseTradeData(string json)
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
                            return ProcessTrade(data.EnumerateArray().First());
                        }
                        else
                        {
                            // Update (один трейд)
                            return ProcessTrade(data);
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
