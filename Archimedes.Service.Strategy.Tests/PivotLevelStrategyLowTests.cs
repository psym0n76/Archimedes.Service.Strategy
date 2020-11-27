using System;
using System.Collections.Generic;
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
    public class PivotLevelStrategyLowTests
    {
        private List<Candle> _candles = new List<Candle>();

        [SetUp]
        public void SetUp()
        {
            LoadMockCandles();
        }

        //[TestCase("2020-10-07T22:45:00")]
        [TestCase("2020-10-08T02:30:00")]
        [TestCase("2020-10-08T05:15:00")]
        [TestCase("2020-10-08T11:45:00")]
        public void Should_Calculate_LowPivots_BasedOn_SevenPivotStrategy(DateTime expectedTimestamp)
        {
            var subject = GetSubjectUnderTest();
            var result = subject.Calculate(_candles, 7);

            Assert.IsTrue(result.Any(a => a.TimeStamp == expectedTimestamp));
        }

        [Test]
        public void Should_Calculate_LowPivots_BasedOn_SevenPivotStrategy_And_Return_Four()
        {
            var subject = GetSubjectUnderTest();
            var result = subject.Calculate(_candles, 7);

            Assert.AreEqual(3, result.Count);
        }

        [Test]
        public void Should_Calculate_LowPivots_BasedOn_FivePivotStrategy_And_Return_Seven()
        {
            var subject = GetSubjectUnderTest();
            var result = subject.Calculate(_candles, 5);

            Assert.AreEqual(6, result.Count);
        }


        // [TestCase("2020-10-07T22:45:00")]  ignore because not enough history
        [TestCase("2020-10-08T02:30:00")]
        [TestCase("2020-10-08T05:15:00")]
        [TestCase("2020-10-08T07:15:00")]
        [TestCase("2020-10-08T09:00:00")]
        [TestCase("2020-10-08T11:45:00")]
        [TestCase("2020-10-08T20:30:00")] // this is a pivot because we only have 24 hour period
        public void Should_Calculate_LowPivots_BasedOn_FivePivotStrategy(DateTime expectedTimestamp)
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

        private static ICandleLoader GetCandleLoader()
        {

            return new CandleLoader();
        }
        
        private static PivotLevelStrategyLow GetSubjectUnderTest()
        {
            var mockPriceLowLogger = new Mock<ILogger<PivotLevelStrategyLow>>();

            return new PivotLevelStrategyLow(mockPriceLowLogger.Object);
        }
    }
}