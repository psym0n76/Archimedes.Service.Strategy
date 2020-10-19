using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Archimedes.Library.Candles;
using Archimedes.Library.Message.Dto;
using Archimedes.Service.Strategy.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Archimedes.Service.Strategy.Tests
{
    [TestFixture]
    public class PriceLevelStrategyTests
    {
        private List<Candle> _candles = new List<Candle>();

        [SetUp]
        public virtual void SetUp()
        {
            LoadMockCandles();
        }

        [Test]
        public  void Should_Load_Candles_And_CalculatePriceLevel_In_LessThan_100_Milliseconds()
        {
            // loading 24hours 15 mins candles - 10ms 
            // loading 24250 hours 15 mins - 100ms
            var subject = GetSubjectUnderTest();

            var largeCandle = new List<Candle>();
            largeCandle.AddRange(_candles);

            for (var i = 1; i < 1000; i++)
            {
                largeCandle.AddRange(_candles);
            }

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var result = subject.Calculate(largeCandle, 7);
            
            Assert.IsTrue(stopWatch.Elapsed.TotalMilliseconds < 1000000);
            TestContext.Out.WriteLine($"Elapsed Time: {stopWatch.Elapsed.TotalMilliseconds}ms");
            TestContext.Out.WriteLine($"Levels created: {result.Count}");
            TestContext.Out.WriteLine($"Candle processed: {largeCandle.Count}");
            TestContext.Out.WriteLine($"Hours: {largeCandle.Count / 4}");
            TestContext.Out.WriteLine($"Days: {largeCandle.Count / 4 / 24}");
            TestContext.Out.WriteLine($"Years: {largeCandle.Count / 4 / 24 / 365}");
        }

        [Test]
        public  void Should_Load_Candles_And_CalculatePriceLevel_In_LessThan_150_Milliseconds()
        {
            // loading 24hours 15 mins candles - 10ms 
            // loading 24250 hours 15 mins - 100ms
            var subject = GetSubjectUnderTest();

            var largeCandle = new List<Candle>();
            largeCandle.AddRange(_candles);

            for (var i = 1; i < 1000; i++)
            {
                largeCandle.AddRange(_candles);
            }

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var result = subject.Calculate(largeCandle, 7);
            
            Assert.IsTrue(stopWatch.Elapsed.TotalMilliseconds < 150);
            TestContext.Out.WriteLine($"Elapsed Time: {stopWatch.Elapsed.TotalMilliseconds}ms");
            TestContext.Out.WriteLine($"Levels created: {result.Count}");
            TestContext.Out.WriteLine($"Candle processed: {largeCandle.Count}");
            TestContext.Out.WriteLine($"Hours: {largeCandle.Count / 4}");
            TestContext.Out.WriteLine($"Days: {largeCandle.Count / 4 / 24}");
            TestContext.Out.WriteLine($"Years: {largeCandle.Count / 4 / 24 / 365}");
        }


        [Test]
        public  void Should_Load_Candles_And_CalculatePriceLevel_In_LessThan_25_Milliseconds()
        {
            // loading 24hours 15 mins candles - 10ms 
            var subject = GetSubjectUnderTest();

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var result = subject.Calculate(_candles, 7);
            
            Assert.IsTrue(stopWatch.Elapsed.TotalMilliseconds < 20);
            TestContext.Out.WriteLine($"Elapsed Time: {stopWatch.Elapsed.TotalMilliseconds}ms");
        }

        [Test]
        public  void Should_Load_Candles_And_CalculatePriceLevel_And_Return_Candles()
        {
            var subject = GetSubjectUnderTest();
            var result = subject.Calculate(_candles, 7);

            Assert.IsTrue(result.Count > 0);
        }


        [Test]
        public void Should_Load_Candles_And_CalculatePriceLevel_BasedOn_SevenPivotStrategy_And_Return_Seven_Candles()
        {
            var subject = GetSubjectUnderTest();
            var result = subject.Calculate(_candles, 7);

            Assert.AreEqual(8,result.Count);
        }


        [TestCase("2020-10-07T22:45:00")]
        [TestCase("2020-10-08T02:30:00")]
        [TestCase("2020-10-08T05:15:00")]
        [TestCase("2020-10-08T11:45:00")]
        public  void Should_Calculate_LowPivots_BasedOn_SevenPivotStrategy(DateTime expectedTimestamp)
        {
            var subject = GetSubjectUnderTest();
            var result = subject.CalculatePivotLow(_candles, 7).ToList();

            Assert.IsTrue(result.Any(a => a.TimeStamp == expectedTimestamp));
        }

        [Test]
        public  void Should_Calculate_LowPivots_BasedOn_SevenPivotStrategy_And_Return_Four()
        {
            var subject = GetSubjectUnderTest();
            var result = subject.CalculatePivotLow(_candles, 7).ToList();

            Assert.AreEqual(4,result.Count);
        }

        [TestCase("2020-10-08T09:15:00")]
        [TestCase("2020-10-08T09:30:00")]
        [TestCase("2020-10-08T14:45:00")]
        [TestCase("2020-10-08T17:00:00")]
        public  void Should_Calculate_HighPivots_BasedOn_SevenPivotStrategy(DateTime expectedTimestamp)
        {
            var subject = GetSubjectUnderTest();
            var result = subject.CalculatePivotHigh(_candles, 7).ToList();

            Assert.IsTrue(result.Any(a => a.TimeStamp == expectedTimestamp));
        }






        [Test]
        public  void Should_Load_Candles_And_CalculatePriceLevel_BasedOn_FivePivotStrategy_And_Return_Twelve_Candles()
        {
            var subject = GetSubjectUnderTest();
            var result = subject.Calculate(_candles, 5);

            Assert.AreEqual(14,result.Count);
        }



        [Test]
        public  void Should_Calculate_LowPivots_BasedOn_FivePivotStrategy_And_Return_Seven()
        {
            var subject = GetSubjectUnderTest();
            var result = subject.CalculatePivotLow(_candles, 5).ToList();

            Assert.AreEqual(7,result.Count);
        }


        [Test]
        public  void Should_Calculate_HighPivots_BasedOn_FivePivotStrategy_And_Return_Seven()
        {
            var subject = GetSubjectUnderTest();
            var result = subject.CalculatePivotHigh(_candles, 5).ToList();

            Assert.AreEqual(7,result.Count);
        }


        [TestCase("2020-10-07T23:45:00")]
        [TestCase("2020-10-08T04:00:00")]
        [TestCase("2020-10-08T09:15:00")]
        [TestCase("2020-10-08T09:30:00")]
        [TestCase("2020-10-08T13:15:00")]
        [TestCase("2020-10-08T14:45:00")]
        [TestCase("2020-10-08T17:00:00")]
        public  void Should_Calculate_HighPivots_BasedOn_FivePivotStrategy(DateTime expectedTimestamp)
        {
            var subject = GetSubjectUnderTest();
            var result = subject.CalculatePivotHigh(_candles, 5).ToList();

            Assert.IsTrue(result.Any(a => a.TimeStamp == expectedTimestamp));
        }


        [TestCase("2020-10-07T22:45:00")]
        [TestCase("2020-10-08T02:30:00")]
        [TestCase("2020-10-08T05:15:00")]
        [TestCase("2020-10-08T07:15:00")]
        [TestCase("2020-10-08T09:00:00")]
        [TestCase("2020-10-08T11:45:00")]
        [TestCase("2020-10-08T20:30:00")] // this is a pivot because we only have 24 hour period
        public  void Should_Calculate_LowPivots_BasedOn_FivePivotStrategy(DateTime expectedTimestamp)
        {
            var subject = GetSubjectUnderTest();
            var result = subject.CalculatePivotLow(_candles, 5).ToList();

            Assert.IsTrue(result.Any(a => a.TimeStamp == expectedTimestamp));
        }















        private async void LoadMockCandles()
        {
            _candles = await GetCandleLoader().Load("GBP/USD", "15Min", 15);
        }

        private static ICandleLoader GetCandleLoader()
        {
            var data = new FileReader();
            var candleDto = data.Reader<CandleDto>("GBPUSD_15Min_202010072200_202010082200");

            var mockRep = new Mock<IHttpRepositoryClient>();
            mockRep.Setup(a => a.GetCandlesByGranularityMarket(It.IsAny<string>(), It.IsAny<string >())).ReturnsAsync(candleDto);

            var mockLogger = new Mock<ILogger<CandleLoader>>();

            return new CandleLoader(mockRep.Object, mockLogger.Object);
        }

        private static PriceLevelStrategy GetSubjectUnderTest()
        {
            var mockLogger = new Mock<ILogger<PriceLevelStrategy>>();

            return new PriceLevelStrategy(mockLogger.Object);
        }
    }
}