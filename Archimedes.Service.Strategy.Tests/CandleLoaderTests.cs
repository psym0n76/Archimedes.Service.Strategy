using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Archimedes.Library.Candles;
using Archimedes.Library.Domain;
using Archimedes.Library.Message.Dto;
using Archimedes.Service.Ui.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using Moq;
using NuGet.Frameworks;
using NUnit.Framework;

namespace Archimedes.Service.Strategy.Tests
{
    public class CandleLoaderTests
    {

        [Test]
        public async Task Should_loadCandle_fromApi()
        {
            var subject = GetSubjectUnderTest();

            var result = await subject.Load("GBP/USD", "15Min", 15);

            Assert.AreEqual(97,result.Count);
        }

        [Test]
        public async Task Should_add_fifteen_candles_to_history_collection()
        {
            var subject = GetSubjectUnderTest();
            var result = await subject.Load("GBP/USD", "15Min", 15);

            var historyCount = result
                .SelectMany(a => a.HistoryCandles).Count(a => a.TimeStamp == new DateTime(2020, 10, 08, 01, 30, 00));

            Assert.AreEqual(15, historyCount);
        }

        [Test]
        public async Task Should_add_fifteen_candles_with_the_first_Candle_startdate_one_interval_behind()
        {
            var subject = GetSubjectUnderTest();
            var result = await subject.Load("GBP/USD", "15Min", 15);


            var history = result.Where(a => a.TimeStamp == new DateTime(2020, 10, 08, 01, 30, 00));


            var maxDate = history.Select(a => a.HistoryCandles.Select(a =>  a.TimeStamp).Max()).FirstOrDefault();
            var minDate = history.Select(a => a.HistoryCandles.Select(a => a.TimeStamp).Min()).FirstOrDefault();


            Assert.AreEqual(new DateTime(2020, 10, 07, 22, 15, 00), minDate);
            Assert.AreEqual(new DateTime(2020, 10, 08, 01, 15, 00), maxDate);
        }



        private ICandleLoader GetSubjectUnderTest()
        {
            //var config = new Config()
            //{
            //    ApiRepositoryUrl = "http://ui-service.dev.archimedes.com/api/"
            //};
            //var mockConfig = new Mock<IOptions<Config>>();
            //var mockLogger = new Mock<ILogger<HttpRepositoryClient>>();
            //var mockHttp = new HttpRepositoryClient(mockConfig.Object, new HttpClient(), mockLogger.Object);
            //mockConfig.Setup(a => a.Value).Returns(config);

            var data = new FileReader();
            var candleDto = data.Reader<CandleDto>("GBPUSD_15Min_202010072200_202010082200");

            var mockRep = new Mock<IHttpRepositoryClient>();
            mockRep.Setup(a => a.GetCandlesByGranularityMarket(It.IsAny<string>(), It.IsAny<string >())).ReturnsAsync(candleDto);

            //_testData = new CandleLoader(mockRep.Object);

            return new CandleLoader(mockRep.Object);
        }
    }
}