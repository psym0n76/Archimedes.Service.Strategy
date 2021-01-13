using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Archimedes.Library.Domain;
using Archimedes.Library.Extensions;
using Archimedes.Library.Logger;
using Archimedes.Library.Message.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Archimedes.Service.Strategy.Http
{
    public class HttpRepositoryClient : IHttpRepositoryClient
    {
        private readonly ILogger<HttpRepositoryClient> _logger;
        private readonly HttpClient _client;
        private readonly BatchLog _batchLog = new();
        private string _logId;

        public HttpRepositoryClient(IOptions<Config> config, HttpClient httpClient,
            ILogger<HttpRepositoryClient> logger)
        {
            httpClient.BaseAddress = new Uri($"{config.Value.ApiRepositoryUrl}");
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            _client = httpClient;
            _logger = logger;
        }

        public async Task<List<CandleDto>> GetCandles()
        {
            _logId = _batchLog.Start();
            _batchLog.Update(_logId,"GET GetCandles");
            
            var response = await _client.GetAsync("candle");

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsAsync<string>();

                if (response.RequestMessage != null)
                    _logger.LogError(_batchLog.Print(_logId, $"GET Failed: {response.ReasonPhrase}  \n\n{errorResponse} \n\n{response.RequestMessage.RequestUri}"));
                return new List<CandleDto>();
            }
            
            var candles = await response.Content.ReadAsAsync<List<CandleDto>>();
            
            _logger.LogInformation(_batchLog.Print(_logId,$"Returned {candles.Count} Candle(s)"));
            return candles;
        }

        public async Task<List<StrategyDto>> GetStrategiesByGranularityMarket(string market, string granularity)
        {
            _logId = _batchLog.Start();
            _batchLog.Update(_logId, $"GET GetStrategiesByGranularityMarket {market} {granularity}");

            var response =
                await _client.GetAsync($"strategy/bymarket_bygranularity?market={market}&granularity={granularity}");

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsAsync<string>();

                if (response.RequestMessage != null)
                    _logger.LogError(_batchLog.Print(_logId, $"GET Failed: {response.ReasonPhrase}  \n\n{errorResponse} \n\n{response.RequestMessage.RequestUri}"));
                return new List<StrategyDto>();
            }

            var strategies =  await response.Content.ReadAsAsync<List<StrategyDto>>();
            _logger.LogInformation(_batchLog.Print(_logId, $"Returned {strategies.Count} Strategies"));
            return strategies;
        }

        public async Task<List<CandleDto>> GetCandlesByGranularityMarket(string market, string granularity)
        {
            _logId = _batchLog.Start();
            _batchLog.Update(_logId, $"GET GetCandlesByGranularityMarket {market} {granularity}");

            var response =
                await _client.GetAsync($"candle/bymarket_bygranularity?market={market}&granularity={granularity}");

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsAsync<string>();

                if (response.RequestMessage != null)
                    _logger.LogError(_batchLog.Print(_logId, $"GET Failed: {response.ReasonPhrase}  \n\n{errorResponse} \n\n{response.RequestMessage.RequestUri}"));
                return new List<CandleDto>();
            }

            var candles = await response.Content.ReadAsAsync<List<CandleDto>>();

            _logger.LogInformation(_batchLog.Print(_logId, $"Returned {candles.Count} Candle(s)"));
            return candles;
        }

        public async Task<List<CandleDto>> GetCandlesByGranularityMarketByDate(string market, string granularity,
            DateTime startDate, DateTime endDate)
        {
            _logId = _batchLog.Start();
            _batchLog.Update(_logId, $"GET GetCandlesByGranularityMarketByDate {market} {granularity}");
            
            var response =
                await _client.GetAsync(
                    $"candle/bymarket_bygranularity_fromdate_todate?market={market}&granularity={granularity}&fromdate={startDate}&todate={endDate}");

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsAsync<string>();

                if (response.RequestMessage != null)
                    _logger.LogError(_batchLog.Print(_logId, $"GET Failed: {response.ReasonPhrase}  \n\n{errorResponse} \n\n{response.RequestMessage.RequestUri}"));
                return new List<CandleDto>();
            }

            var candles = await response.Content.ReadAsAsync<List<CandleDto>>();

            _logger.LogInformation(_batchLog.Print(_logId, $"Returned {candles.Count} Candle(s)"));
            return candles;
        }

        public async Task<PriceLevelDto> AddPriceLevel(PriceLevelDto priceLevel)
        {
            _logId = _batchLog.Start();
            _batchLog.Update(_logId,
                $"POST AddPriceLevel {priceLevel.BuySell} {priceLevel.Market} {priceLevel.TimeStamp}");

            var payload = new JsonContent(priceLevel);
            var response = _client.PostAsync("price-level", payload).Result; //post request wait to finish

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsAsync<string>();

                if (response.RequestMessage != null)
                {
                    if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
                    {
                        _logger.LogInformation(_batchLog.Print(_logId, $"POST Failed Duplicate: {response.RequestMessage.RequestUri}"));
                        return new PriceLevelDto(){Strategy = "Duplicate"};
                    }

                    _logger.LogError(_batchLog.Print(_logId,$"POST Failed: {response.ReasonPhrase}  \n\n{errorResponse} \n\n{response.RequestMessage.RequestUri}"));
                    return new PriceLevelDto();
                }
            }
            
            var level = await response.Content.ReadAsAsync<PriceLevelDto>();

            _logger.LogInformation(_batchLog.Print(_logId, $"ADDED PriceLevel ID={level.Id}"));
            return level;
        }

        public async void UpdateStrategyMetrics(StrategyDto strategy)
        {
            _logId = _batchLog.Start();
            _batchLog.Update(_logId,
                $"PUT UpdateStrategyMetrics {strategy.Name} {strategy.Market} {strategy.Granularity}");

            var payload = new JsonContent(strategy);
            var response = await _client.PutAsync("strategy", payload);

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsAsync<string>();

                if (response.RequestMessage != null)
                    _logger.LogError(_batchLog.Print(_logId, $"PUT Failed: {response.ReasonPhrase}  \n\n{errorResponse} \n\n{response.RequestMessage.RequestUri}"));
                return;
            }
            
            _logger.LogInformation(_batchLog.Print(_logId, $"UPDATED Strategy Statistics {strategy.Name} {strategy.Market} {strategy.LastUpdated}"));
        }
    }
}
