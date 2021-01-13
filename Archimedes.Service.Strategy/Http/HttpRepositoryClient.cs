using System;
using System.Collections.Generic;
using System.Net;
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
            var response = await _client.GetAsync("candle");

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsAsync<string>();

                if (response.RequestMessage != null)
                    _logger.LogError(
                        $"GET Failed: {response.ReasonPhrase}  \n\n{errorResponse} \n\n{response.RequestMessage.RequestUri}");
                return new List<CandleDto>();
            }

            return await response.Content.ReadAsAsync<List<CandleDto>>();
        }

        public async Task<List<StrategyDto>> GetStrategiesByGranularityMarket(string market, string granularity)
        {
            var response =
                await _client.GetAsync($"strategy/bymarket_bygranularity?market={market}&granularity={granularity}");

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsAsync<string>();

                if (response.RequestMessage != null)
                    _logger.LogError(
                        $"GET Failed: {response.ReasonPhrase}  \n\n{errorResponse} \n\n{response.RequestMessage.RequestUri}");
                return new List<StrategyDto>();
            }

            return await response.Content.ReadAsAsync<List<StrategyDto>>();
        }

        public async Task<List<CandleDto>> GetCandlesByGranularityMarket(string market, string granularity)
        {
            var response =
                await _client.GetAsync($"candle/bymarket_bygranularity?market={market}&granularity={granularity}");

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsAsync<string>();

                if (response.RequestMessage != null)
                    _logger.LogError(
                        $"GET Failed: {response.ReasonPhrase}  \n\n{errorResponse} \n\n{response.RequestMessage.RequestUri}");
                return new List<CandleDto>();
            }

            return await response.Content.ReadAsAsync<List<CandleDto>>();
        }

        public async Task<List<CandleDto>> GetCandlesByGranularityMarketByDate(string market, string granularity,
            DateTime startDate, DateTime endDate)
        {
            var response =
                await _client.GetAsync(
                    $"candle/bymarket_bygranularity_fromdate_todate?market={market}&granularity={granularity}&fromdate={startDate}&todate={endDate}");

            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsAsync<string>();

                if (response.RequestMessage != null)
                    _logger.LogError(
                        $"GET Failed: {response.ReasonPhrase}  \n\n{errorResponse} \n\n{response.RequestMessage.RequestUri}");
                return new List<CandleDto>();
            }

            return await response.Content.ReadAsAsync<List<CandleDto>>();
        }

        public async Task<PriceLevelDto> AddPriceLevel(PriceLevelDto priceLevel)
        {
            try
            {
                var payload = new JsonContent(priceLevel);
                var response = _client.PostAsync("price-level", payload).Result; //post request wait to finish

                if (!response.IsSuccessStatusCode)
                {
                    var errorResponse = await response.Content.ReadAsAsync<string>();

                    if (response.RequestMessage != null)
                    {
                        if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
                        {
                            _logger.LogInformation(
                                $"POST Failed: {response.RequestMessage}");
                            return new PriceLevelDto();
                        }

                        _logger.LogError(
                            $"POST Failed: {response.ReasonPhrase}  \n\n{errorResponse} \n\n{response.RequestMessage.RequestUri}");
                        return new PriceLevelDto();
                    }
                }

                _logger.LogInformation($"ADDED PriceLevels");

                return await response.Content.ReadAsAsync<PriceLevelDto>();

            }
            catch (Exception e)
            {
                _logger.LogError($"Error {e.Message} {e.StackTrace}");
                return new PriceLevelDto();
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
                    var errorResponse = await response.Content.ReadAsAsync<string>();

                    if (response.RequestMessage != null)
                        _logger.LogError(
                            $"PUT Failed: {response.ReasonPhrase}  \n\n{errorResponse} \n\n{response.RequestMessage.RequestUri}");
                    return;
                }

                _logger.LogInformation(
                    $"UPDATED Strategy Statistics {strategy.Name} {strategy.Market} {strategy.LastUpdated}\n");
            }
            catch (Exception e)
            {
                _logger.LogError($"Error {e.Message} {e.StackTrace}");
            }
        }
    }
}
