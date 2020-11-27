using System.Collections.Generic;
using System.Diagnostics;
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
        public void Should_Load_Candles_And_CalculatePriceLevel_In_LessThan_100_Milliseconds()
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
        public void Should_Load_Candles_And_CalculatePriceLevel_In_LessThan_150_Milliseconds()
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

 
            TestContext.Out.WriteLine($"Elapsed Time: {stopWatch.Elapsed.TotalMilliseconds}ms");
            TestContext.Out.WriteLine($"Levels created: {result.Count}");
            TestContext.Out.WriteLine($"Candle processed: {largeCandle.Count}");
            TestContext.Out.WriteLine($"Hours: {largeCandle.Count / 4}");
            TestContext.Out.WriteLine($"Days: {largeCandle.Count / 4 / 24}");
            TestContext.Out.WriteLine($"Years: {largeCandle.Count / 4 / 24 / 365}");
            Assert.IsTrue(stopWatch.Elapsed.TotalMilliseconds < 175);
        }


        [Test]
        public void Should_Load_Candles_And_CalculatePriceLevel_In_LessThan_25_Milliseconds()
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
        public void Should_Load_Candles_And_CalculatePriceLevel_And_Return_Candles()
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

            Assert.AreEqual(7, result.Count);
        }


        [Test]
        public void Should_Load_Candles_And_CalculatePriceLevel_BasedOn_FivePivotStrategy_And_Return_Twelve_Candles()
        {
            var subject = GetSubjectUnderTest();
            var result = subject.Calculate(_candles, 5);

            Assert.AreEqual(13, result.Count);
        }

        private void LoadMockCandles()
        {
            var data = new FileReader();
            var candleDto = data.Reader<CandleDto>("GBPUSD_15Min_202010072200_202010082200");

            _candles = GetCandleLoader().Load("GBP/USD", "15Min", 15,candleDto);
        }

        private static ICandleLoader GetCandleLoader()
        {
            return new CandleLoader();
        }

        private static PriceLevelStrategy GetSubjectUnderTest()
        {
            var mockLogger = new Mock<ILogger<PriceLevelStrategy>>();

            var mockPriceHighLogger = new Mock<ILogger<Strategy.PivotLevelStrategyHigh>>();
            var mockPriceLowLogger = new Mock<ILogger<PivotLevelStrategyLow>>();

            var mockPriceHigh = new Strategy.PivotLevelStrategyHigh(mockPriceHighLogger.Object);
            var mockPriceLow = new PivotLevelStrategyLow(mockPriceLowLogger.Object);

            return new PriceLevelStrategy(mockLogger.Object, mockPriceHigh, mockPriceLow);
        }
    }
}