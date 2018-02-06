using System;
using System.Collections.Generic;
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
        public void LogVersionTest()
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
            IReadOnlyCollection<ExceptionReport> reports = BigWatson.Instance.LoadExceptionsAsync(BigWatson.CurrentAppVersion).Result;
            Assert.IsTrue(reports.Count == 1);
            Assert.IsTrue(reports.First().ExceptionType.Equals(typeof(InvalidOperationException).ToString()));
            Assert.IsTrue(reports.First().Message.Equals("Hello world!"));
        }

        [TestMethod]
        public void LogPredicateTest()
        {
            // Log
            BigWatson.Instance.ResetAsync().Wait();
            Exception[] exceptions =
            {
                new InvalidOperationException("Hello world!"),
                new ArithmeticException("Division by zero"),
                new InvalidOperationException("We're being too lazy here!"),
            };
            foreach (Exception exception in exceptions)
            {
                try
                {
                    throw exception;
                }
                catch (Exception e)
                {
                    BigWatson.Instance.Log(e);
                }
            }

            // Checks
            LogsCollection<ExceptionReport> reports = BigWatson.Instance.LoadExceptionsAsync(entry => entry.ExceptionType.Equals(typeof(InvalidOperationException).ToString())).Result;
            Assert.IsTrue(reports.LogsCount == 2);
            Assert.IsTrue(reports[0][1].Message.Equals(exceptions[0].Message));
        }

        [TestMethod]
        public void MemoryParserTest()
        {
            // Log
            BigWatson.Instance.ResetAsync().Wait();
            BigWatson.MemoryParser = () => 128L;
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
