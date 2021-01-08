using System;
using System.Collections.Generic;
using System.Linq;
using Archimedes.Library.Candles;
using Archimedes.Library.Message.Dto;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Archimedes.Service.Strategy.Tests
{
    [TestFixture]
    public class PivotLevelStrategyHighTests
    {
        private List<Candle> _candles = new List<Candle>();

        [SetUp]
        public  void SetUp()
        {
            LoadMockCandles();
        }

        [TestCase("2020-10-08T09:15:00")]
        [TestCase("2020-10-08T09:30:00")]
        [TestCase("2020-10-08T14:45:00")]
        [TestCase("2020-10-08T17:00:00")]
        public void Should_Calculate_HighPivots_BasedOn_SevenPivotStrategy(DateTime expectedTimestamp)
        {
            var subject = GetSubjectUnderTest();
            var result = subject.Calculate(_candles, 7);

            Assert.IsTrue(result.Any(a => a.TimeStamp == expectedTimestamp));
        }

        [Test]
        public void Should_Calculate_HighPivots_BasedOn_FivePivotStrategy_And_Return_Seven()
        {
            var subject = GetSubjectUnderTest();
            var result = subject.Calculate(_candles, 5);

            Assert.AreEqual(7, result.Count);
        }

        // [TestCase("2020-10-07T23:45:00")] ignore because not enough history
        [TestCase("2020-10-08T04:00:00")]
        [TestCase("2020-10-08T09:15:00")]
        [TestCase("2020-10-08T09:30:00")]
        [TestCase("2020-10-08T13:15:00")]
        [TestCase("2020-10-08T14:45:00")]
        [TestCase("2020-10-08T17:00:00")]
        public void Should_Calculate_HighPivots_BasedOn_FivePivotStrategy(DateTime expectedTimestamp)
        {
            var subject = GetSubjectUnderTest();
            var result = subject.Calculate(_candles, 5);

            Assert.IsTrue(result.Any(a => a.TimeStamp == expectedTimestamp));
        }

        private void LoadMockCandles()
        {
            var data = new FileReader();
            var candleDto = data.Reader<CandleDto>("GBPUSD_15Min_202010072200_202010082200");
            _candles = GetCandleLoader().Load(candleDto);
        }

        private static ICandleHistoryLoader GetCandleLoader()
        {
            return new CandleHistoryLoader();
        }

        private static PivotLevelStrategyHigh GetSubjectUnderTest()
        {
            var mockPriceHighLogger = new Mock<ILogger<PivotLevelStrategyHigh>>();

            return new PivotLevelStrategyHigh(mockPriceHighLogger.Object);
        }
    }
}