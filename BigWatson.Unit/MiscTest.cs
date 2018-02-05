using System;
using System.Linq;
using BigWatsonDotNet.Enums;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BigWatsonDotNet.Unit
{
    [TestClass]
    [TestCategory(nameof(MiscTest))]
    public class MiscTest
    {
        [TestMethod]
        public void LogTest()
        {
            BigWatson.Instance.ResetAsync().Wait();
            long before = BigWatson.Instance.Size;
            for (int i = 0; i < 200; i++)
            {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
                Random random = new Random(Environment.TickCount);
                var message = new string(Enumerable.Range(0, 500).Select(_ => chars[random.Next() % chars.Length]).ToArray());
                BigWatson.Instance.Log(EventPriority.Debug, message);
            }
            long after = BigWatson.Instance.Size;
            Assert.IsTrue(after > before);
        }
    }
}
