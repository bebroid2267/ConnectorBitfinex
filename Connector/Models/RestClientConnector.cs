using RestSharp;

namespace Connector.Models
{
    public class RestClientConnector : IDisposable
    {
        private readonly RestClient _client;

        public RestClientConnector()
        {
            var options = new RestClientOptions("https://api-pub.bitfinex.com/v2/");
            _client = new RestClient(options);
        }

        public async Task<Trade> GetTrades(string pair) 
        {
            var response = await _client.GetJsonAsync<Trade>($"trades/{pair}/hist?limit=125&sort=-1");
            return response;
        }

        public async Task<Candle> GetCandles(string pair) 
        {
            var response = await _client.GetJsonAsync<Candle>($"candles/trade:1m:{pair}/last");
            return response;

        }
        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
