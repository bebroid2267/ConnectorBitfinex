using RestSharp;

namespace Connector.Models
{
    public class RestClient : IDisposable
    {
        private readonly RestSharp.RestClient _client;

        public RestClient()
        {
            var options = new RestClientOptions("https://api-pub.bitfinex.com/v2/");
            _client = new RestSharp.RestClient(options);
        }

        public async Task<List<Trade>> GetTrades(string pair, int limit) 
        {
            var response = await _client.GetJsonAsync<List<List<object>>>($"trades/{pair}/hist?limit={limit}&sort=-1");

            return response.Select(trade => new Trade
            {
                Id = trade[0].ToString(),
                Time = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(trade[1])),
                Amount = Convert.ToDecimal(trade[2]),
                Price = Convert.ToDecimal(trade[3]),
                Pair = pair
            }).ToList();
        }

        public async Task<List<Candle>> GetCandles(
            string pair,
            string timeFrame = "1m",
            int periodInSec = 30,
            DateTimeOffset? from = null,
            DateTimeOffset? to = null,
            long? count = 100)
        {
            // Формируем строку периода
            string periodStr = $"p{periodInSec}";

            // Агрегированный ключ, например, `a10`
            string aggregateKey = "a30"; // Можно сделать параметром, если надо менять

            // Базовый URL
            string url = $"candles/trade:{timeFrame}:{pair}:{aggregateKey}:{periodStr}/{(count > 1 ? "hist" : "last")}";

            // Добавляем временные ограничения и лимит
            var queryParams = new List<string>();

            if (from.HasValue)
                queryParams.Add($"start={from.Value.ToUnixTimeMilliseconds()}");

            if (to.HasValue)
                queryParams.Add($"end={to.Value.ToUnixTimeMilliseconds()}");

            if (count.HasValue && count > 0)
                queryParams.Add($"limit={count}");

            if (queryParams.Any())
                url += "?" + string.Join("&", queryParams);

            // Выполняем запрос
            var response = await _client.GetJsonAsync<List<List<object>>>(url);

            // Преобразуем ответ в список объектов Candle
            var candles = response.Select(c => new Candle
            {
                Pair = pair,
                OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(c[0])),
                OpenPrice = Convert.ToDecimal(c[1]),
                ClosePrice = Convert.ToDecimal(c[2]),
                HighPrice = Convert.ToDecimal(c[3]),
                LowPrice = Convert.ToDecimal(c[4]),
                TotalVolume = Convert.ToDecimal(c[5])
            }).ToList();

            return candles;
        }
        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
