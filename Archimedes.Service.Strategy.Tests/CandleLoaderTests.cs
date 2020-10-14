using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Archimedes.Library.Candles;
using Archimedes.Library.Message.Dto;
using Archimedes.Service.Ui.Http;
using Moq;
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
        public async Task Should_add_thirty_candles_to_history_collection()
        {
            var subject = GetSubjectUnderTest();
            var result = await subject.Load("GBP/USD", "15Min", 15);

            var historyCount = result
                .SelectMany(a => a.HistoryCandles).Count(a => a.TimeStamp == new DateTime(2020, 10, 08, 01, 45, 00));

            Assert.AreEqual(30, historyCount);
        }

        [Test]
        public async Task Should_add_current_candle_to_history()
        {
            var subject = GetSubjectUnderTest();
            var candles = await subject.Load("GBP/USD", "15Min", 15);

            const string currentFromDate = "2020-10-08T01:45:00";
            const string currentToDate = "2020-10-08T02:00:00";

            var historyCollection = new List<Candle>();

            foreach (var candle in candles.Where(h => h.TimeStamp == new DateTime(2020, 10, 08, 01, 45, 00)))
            {
                historyCollection.AddRange(candle.HistoryCandles);
            }

            Assert.IsTrue(historyCollection.Any(a => a.TimeStamp == DateTime.Parse(currentFromDate)));
            Assert.IsTrue(historyCollection.Any(a => a.TimeStamp == DateTime.Parse(currentToDate)));
        }

        [Test]
        public async Task Should_add_thirty_candles_with_max_and_min_candles()
        {
            var subject = GetSubjectUnderTest();
            var result = await subject.Load("GBP/USD", "15Min", 15);


            var history = result.Where(a => a.TimeStamp == new DateTime(2020, 10, 08, 01, 45, 00));


            var maxDate = history.Select(a => a.HistoryCandles.Select(b =>  b.TimeStamp).Max()).FirstOrDefault();
            var minDate = history.Select(a => a.HistoryCandles.Select(b => b.TimeStamp).Min()).FirstOrDefault();


            Assert.AreEqual(new DateTime(2020, 10, 07, 22, 15, 00), minDate);
            Assert.AreEqual(new DateTime(2020, 10, 08, 05, 30, 00), maxDate);
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