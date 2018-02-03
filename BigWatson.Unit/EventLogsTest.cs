using System.Linq;
using BigWatsonDotNet.Enums;
using BigWatsonDotNet.Models;
using BigWatsonDotNet.Models.Events;
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
    }
}
