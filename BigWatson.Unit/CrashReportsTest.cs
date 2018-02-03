using System;
using System.Linq;
using BigWatsonDotNet.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BigWatsonDotNet.Unit
{
    [TestClass]
    [TestCategory(nameof(CrashReportsTest))]
    public class CrashReportsTest
    {
        [TestMethod]
        public void LogTest()
        {
            // Log
            BigWatson.Instance.ResetAsync().Wait();
            try
            {
                throw new InvalidOperationException("Hello world!");
            }
            catch (Exception e)
            {
                BigWatson.Instance.Log(e);
            }

            // Checks
            LogsCollection<ExceptionReport> reports = BigWatson.Instance.LoadExceptionsAsync().Result;
            Assert.IsTrue(reports.LogsCount == 1);
            Assert.IsTrue(reports.Logs.First().ExceptionType.Equals(typeof(InvalidOperationException).ToString()));
            Assert.IsTrue(reports.Logs.First().Message.Equals("Hello world!"));
            Assert.IsTrue(DateTime.Now.Subtract(reports.Logs.First().Timestamp) < TimeSpan.FromMinutes(1));
        }

        [TestMethod]
        public void LogThresholdTest()
        {
            // Log
            BigWatson.Instance.ResetAsync().Wait();
            try
            {
                throw new InvalidOperationException("Hello world!");
            }
            catch (Exception e)
            {
                BigWatson.Instance.Log(e);
            }

            // Checks
            LogsCollection<ExceptionReport> reports = BigWatson.Instance.LoadExceptionsAsync(TimeSpan.FromMinutes(1)).Result;
            Assert.IsTrue(reports.LogsCount == 1);
            Assert.IsTrue(reports.Logs.First().ExceptionType.Equals(typeof(InvalidOperationException).ToString()));
            Assert.IsTrue(reports.Logs.First().Message.Equals("Hello world!"));
            Assert.IsTrue(DateTime.Now.Subtract(reports.Logs.First().Timestamp) < TimeSpan.FromMinutes(1));
        }

        [TestMethod]
        public void MemoryParserTest()
        {
            // Log
            BigWatson.Instance.ResetAsync().Wait();
            BigWatson.UsedMemoryParser = () => 128L;
            try
            {
                throw new InvalidOperationException();
            }
            catch (Exception e)
            {
                BigWatson.Instance.Log(e);
            }

            // Checks
            LogsCollection<ExceptionReport> reports = BigWatson.Instance.LoadExceptionsAsync().Result;
            Assert.IsTrue(reports.Logs.First().UsedMemory == 128L);
        }
    }
}
