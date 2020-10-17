using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Archimedes.Library.Candles;
using Archimedes.Library.Message.Dto;
using Archimedes.Service.Ui.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Archimedes.Service.Strategy.Tests
{
    public class CandleLoaderTests
    {
        
        [Test]
        public async Task Should_LoadCandle_And_First_TimeStamp_Should_Match()
        {
            var subject = GetSubjectUnderTest();

            var result = await subject.Load("GBP/USD", "15Min", 15);

            Assert.AreEqual(new DateTime(2020, 10, 07, 22, 00, 00), result.Take(1).Select(a => a.TimeStamp).Single());
        }


        [Test]
        public async Task Should_LoadCandle()
        {
            var subject = GetSubjectUnderTest();

            var result = await subject.Load("GBP/USD", "15Min", 15);

            Assert.AreEqual(97,result.Count);
        }

        [Test]
        public async Task Performance_Should_Load_15Min_Candle_For_OneDay_In_LessThan_25_Milliseconds()
        {
            //average loads 98 rows in 21ms
            // in reality this is 8 - 10ms

            var subject = GetSubjectUnderTest();

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var result = await subject.Load("GBP/USD", "15Min", 15);
            
            Assert.AreEqual(97,result.Count);

            Assert.IsTrue(stopWatch.Elapsed.TotalMilliseconds < 25);
            TestContext.Out.WriteLine($"Elapsed Time: {stopWatch.Elapsed.TotalMilliseconds}ms");
        }

        [Test]
        public async Task Performance_Should_Load_15Min_Candle_For_OneDay_OneHundredTimes()
        {
            //average loads 98 rows in 21ms
            //average 600 for 9800ms
            //average 5544 for 98000ms

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var counter = 0;
            var subject = GetSubjectUnderTest();

            for (var i = 1; i < 100; i++)
            {
                var result = await subject.Load("GBP/USD", "15Min", 15);
                counter = result.Count;
            }
            
            Assert.AreEqual(97,counter);

            Assert.IsTrue(stopWatch.Elapsed.TotalMilliseconds < 750);
            TestContext.Out.WriteLine($"Elapsed Time: {stopWatch.Elapsed.TotalMilliseconds}ms");
        }

        [Test]
        public async Task Should_Add_Fifteen_Candles_To_PastHistory()
        {
            var subject = GetSubjectUnderTest();
            var result = await subject.Load("GBP/USD", "15Min", 15);

            var historyCountPast = result
                .SelectMany(a => a.PastCandles).Count(a => a.TimeStamp == new DateTime(2020, 10, 08, 01, 45, 00));

            Assert.AreEqual(14, historyCountPast);

        }

        [Test]
        public async Task Should_Not_Add_Current_Candle_To_PastHistory()
        {
            var subject = GetSubjectUnderTest();
            var candles = await subject.Load("GBP/USD", "15Min", 15);

            const string currentFromDate = "2020-10-08T01:45:00";

            var historyCollection = new List<Candle>();

            foreach (var candle in candles.Where(h => h.TimeStamp == new DateTime(2020, 10, 08, 01, 45, 00)))
            {
                historyCollection.AddRange(candle.PastCandles);
            }

            Assert.IsFalse(historyCollection.Any(a => a.TimeStamp == DateTime.Parse(currentFromDate)));
        }

        [Test]
        public async Task Should_Load_Candles_In_Ascending_Order()
        {
            var subject = GetSubjectUnderTest();
            var candles = await subject.Load("GBP/USD", "15Min", 15);

            var firstCandle = candles.Take(1).Select(a => a.TimeStamp).First();
            var lastCandle = candles.TakeLast(1).Select(a => a.TimeStamp).First();

            Assert.IsTrue(lastCandle > firstCandle);
        }

        [Test]
        public async Task Should_Add_PastCandles_With_Max_And_Min_Candles_Matching_Result()
        {
            var subject = GetSubjectUnderTest();
            var result = await subject.Load("GBP/USD", "15Min", 15);

            var history = result.Where(a => a.TimeStamp == new DateTime(2020, 10, 08, 01, 45, 00)).ToList();

            var minDate = history.Select(a => a.PastCandles.Select(b => b.TimeStamp).Min()).FirstOrDefault();
            var maxDate = history.Select(a => a.PastCandles.Select(b =>  b.TimeStamp).Max()).FirstOrDefault();

            Assert.AreEqual(new DateTime(2020, 10, 08, 01, 30, 00), maxDate);
            Assert.AreEqual(new DateTime(2020, 10, 07, 22, 15, 00), minDate);
        }

        [Test]
        public async Task Should_Load_PastCandles_In_DescendingOrder()
        {
            var subject = GetSubjectUnderTest();
            var result = await subject.Load("GBP/USD", "15Min", 15);

            var history = result.Where(a => a.TimeStamp == new DateTime(2020, 10, 08, 01, 45, 00)).ToList();

            var historyCandles = history.Select(a => a.PastCandles).Single();

            var timeStamp = historyCandles.Take(1).Select(a=>a.TimeStamp).Single();

            Assert.AreEqual(new DateTime(2020, 10, 08, 01, 30, 00), timeStamp);
        }

        [Test]
        public async Task Should_Load_FutureCandles_In_AscendingOrder()
        {
            var subject = GetSubjectUnderTest();
            var result = await subject.Load("GBP/USD", "15Min", 15);

            var history = result.Where(a => a.TimeStamp == new DateTime(2020, 10, 08, 01, 45, 00)).ToList();

            var futureCandles = history.Select(a => a.FutureCandles).Single();

            var timeStamp = futureCandles.Take(1).Select(a=>a.TimeStamp).Single();

            Assert.AreEqual(new DateTime(2020, 10, 08, 02, 00, 00), timeStamp);
        }

        [Test]
        public async Task Should_Add_FutureCandles_With_Max_And_Min_Candles_Matching_Result()
        {
            var subject = GetSubjectUnderTest();
            var result = await subject.Load("GBP/USD", "15Min", 15);

            var history = result.Where(a => a.TimeStamp == new DateTime(2020, 10, 08, 01, 45, 00)).ToList();

            var minDate = history.Select(a => a.FutureCandles.Select(b => b.TimeStamp).Min()).FirstOrDefault();
            var maxDate = history.Select(a => a.FutureCandles.Select(b =>  b.TimeStamp).Max()).FirstOrDefault();

            Assert.AreEqual(new DateTime(2020, 10, 08, 02, 00, 00), minDate);
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

            var mockLogger = new Mock<ILogger<CandleLoader>>();

            return new CandleLoader(mockRep.Object, mockLogger.Object);
        }
    }
}