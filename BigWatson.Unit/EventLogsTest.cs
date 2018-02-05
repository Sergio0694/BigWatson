using System;
using System.Linq;
using BigWatsonDotNet.Enums;
using BigWatsonDotNet.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BigWatsonDotNet.Unit
{
    [TestClass]
    [TestCategory(nameof(EventLogsTest))]
    public class EventLogsTest
    {
        [TestMethod]
        public void LogTest()
        {
            // Log
            BigWatson.Instance.ResetAsync().Wait();
            BigWatson.Instance.Log(EventPriority.Info, "Some random info");
            BigWatson.Instance.Log(EventPriority.Warning, "Watch out!");

            // Checks
            LogsCollection<Event> reports = BigWatson.Instance.LoadEventsAsync().Result;
            Assert.IsTrue(reports.LogsCount == 2);
            Assert.IsTrue(reports.Logs.First().Priority == EventPriority.Warning);
            Assert.IsTrue(reports.Logs.Skip(1).First().Priority == EventPriority.Info);
        }

        [TestMethod]
        public void LogThresholdTest()
        {
            // Log
            BigWatson.Instance.ResetAsync().Wait();
            BigWatson.Instance.Log(EventPriority.Info, "Some random info");
            BigWatson.Instance.Log(EventPriority.Warning, "Watch out!");

            // Checks
            LogsCollection<Event> reports = BigWatson.Instance.LoadEventsAsync(TimeSpan.FromDays(2)).Result;
            Assert.IsTrue(reports.LogsCount == 2);
            Assert.IsTrue(reports.Logs.First().Priority == EventPriority.Warning);
            Assert.IsTrue(reports.Logs.Skip(1).First().Priority == EventPriority.Info);
        }

        [TestMethod]
        public void RemoveTest()
        {
            // Log
            BigWatson.Instance.ResetAsync().Wait();
            foreach (Exception exception in new Exception[]
            {
                new InvalidOperationException("Hello world!"),
                new ArgumentException("This parameter was too weird to be evaluated")
            })
            {
                BigWatson.Instance.Log(exception);
            }
            BigWatson.Instance.Log(EventPriority.Info, "Some random info");
            BigWatson.Instance.Log(EventPriority.Warning, "Watch out!");

            // Checks
            BigWatson.Instance.ResetAsync<ExceptionReport>();
            LogsCollection<Event> reports = BigWatson.Instance.LoadEventsAsync().Result;
            Assert.IsTrue(reports.LogsCount == 2);
            Assert.IsTrue(reports.Logs.First().Priority == EventPriority.Warning);
            Assert.IsTrue(reports.Logs.Skip(1).First().Priority == EventPriority.Info);
        }
    }
}
