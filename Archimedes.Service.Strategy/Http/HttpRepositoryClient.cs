using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Archimedes.Library.Domain;
using Archimedes.Library.Extensions;
using Archimedes.Library.Message.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Archimedes.Service.Strategy.Http
{
    public class HttpRepositoryClient : IHttpRepositoryClient
    { 
        private readonly ILogger<HttpRepositoryClient> _logger;
        private readonly HttpClient _client;

        public HttpRepositoryClient(IOptions<Config> config, HttpClient httpClient, ILogger<HttpRepositoryClient> logger)
        {
            httpClient.BaseAddress = new Uri($"{config.Value.ApiRepositoryUrl}");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _client = httpClient;
            _logger = logger;
        }

        public async Task<List<CandleDto>> GetCandles()
        {
            var response = await _client.GetAsync("candle");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"GET Failed: {response.ReasonPhrase} from {response.RequestMessage.RequestUri}");
                return null;
            }

            var candles = await response.Content.ReadAsAsync<IEnumerable<CandleDto>>();

            return candles.ToList();
        }



        public async Task<List<StrategyDto>> GetStrategiesByGranularityMarket(string market, string granularity)
        {
            var response = await _client.GetAsync($"strategy/bymarket_bygranularity?market={market}&granularity={granularity}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"GET Failed: {response.ReasonPhrase} from {response.RequestMessage.RequestUri}");
                return null;
            }

            var strategies = await response.Content.ReadAsAsync<IEnumerable<StrategyDto>>();

            return strategies.ToList();
        }

        public async Task<List<CandleDto>> GetCandlesByGranularityMarket(string market, string granularity)
        {
            var response = await _client.GetAsync($"candle/bymarket_bygranularity?market={market}&granularity={granularity}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"GET Failed: {response.ReasonPhrase} from {response.RequestMessage.RequestUri}");
                return null;
            }

            var candles = await response.Content.ReadAsAsync<IEnumerable<CandleDto>>();

            return candles.ToList();
        }

        public async Task<List<CandleDto>> GetCandlesByGranularityMarketByDate(string market, string granularity, DateTime startDate, DateTime endDate)
        {
            var response = await _client.GetAsync($"candle/bymarket_bygranularity_fromdate_todate?market={market}&granularity={granularity}&fromdate={startDate}&todate={endDate}");

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsAsync<string>();
                
                if (response.RequestMessage != null)
                    _logger.LogError(
                        $"GET Failed: {response.ReasonPhrase}  {errorResponse} from {response.RequestMessage.RequestUri}");
                return null;
            }

            var candles = await response.Content.ReadAsAsync<IEnumerable<CandleDto>>();

            return candles.ToList();
        }

        public void AddPriceLevel(List<PriceLevelDto> priceLevel)
        {
            try
            {
                var payload = new JsonContent(priceLevel);
                var response =  _client.PostAsync("price-level", payload).Result; //post request wait to finish

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to Post {response.ReasonPhrase} from {_client.BaseAddress}price-level");
                }

                _logger.LogInformation(
                    $"ADDED PriceLevels {priceLevel} Price Levels\n");
            }
            catch (Exception e)
            {
                _logger.LogError($"Error {e.Message} {e.StackTrace}");
            }
        }

        public async void UpdateStrategyMetrics(StrategyDto strategy)
        {
            try
            {
                var payload = new JsonContent(strategy);
                var response = await _client.PutAsync("strategy", payload);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to Post {response.ReasonPhrase} from {_client.BaseAddress}strategy");
                }

                _logger.LogInformation(
                    $"UPDATED Strategy Statistics {strategy}\n");
            }
            catch (Exception e)
            {
                _logger.LogError($"Error {e.Message} {e.StackTrace}");
            }
        }
    }
}
