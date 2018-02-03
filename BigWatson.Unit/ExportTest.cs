using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BigWatsonDotNet.Enums;
using BigWatsonDotNet.Interfaces;
using BigWatsonDotNet.Models;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BigWatsonDotNet.Unit
{
    [TestClass]
    [TestCategory(nameof(ExportTest))]
    public class ExportTest
    {
        /// <summary>
        /// Gets the path to the local assets folder
        /// </summary>
        [NotNull]
        public static String LocalPath
        {
            get
            {
                String
                    code = Assembly.GetExecutingAssembly().Location,
                    dll = Path.GetFullPath(code),
                    root = Path.GetDirectoryName(dll),
                    path = Path.Combine(root, "Assets");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                return path;
            }
        }

        [TestMethod]
        public void DatabaseExportTest()
        {
            // Log
            BigWatson.Instance.ResetAsync().Wait();
            try
            {
                throw new InvalidOperationException("Export test");
            }
            catch (Exception e)
            {
                BigWatson.Instance.Log(e);
            }
            String path = Path.Combine(LocalPath, $"test{BigWatson.DatabaseExtension}");
            BigWatson.Instance.ExportAsync(path).Wait();

            // Check
            IReadOnlyLogger loaded = BigWatson.Load(path);
            LogsCollection<ExceptionReport> reports = loaded.LoadExceptionsAsync().Result;
            Assert.IsTrue(reports.LogsCount == 1);
            Assert.IsTrue(reports.Logs.First().ExceptionType.Equals(typeof(InvalidOperationException).ToString()));
            Assert.IsTrue(reports.Logs.First().Message.Equals("Export test"));
            File.Delete(path);
        }

        [TestMethod]
        public void JsonExportTest()
        {
            // Log
            BigWatson.Instance.ResetAsync().Wait();
            Exception[] exceptions =
            {
                new InvalidOperationException("Hello world!"),
                new ArithmeticException("Division by zero"),
                new NotImplementedException("We're being too lazy here!"), 
                new ArgumentException("This parameter was too weird to be evaluated")
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
            BigWatson.Instance.Log(EventPriority.Info, "Some random info");
            BigWatson.Instance.Log(EventPriority.Warning, "Watch out!");

            // Checks
            LogsCollection<ExceptionReport> reports = BigWatson.Instance.LoadExceptionsAsync().Result;
            Assert.IsTrue(reports.LogsCount == exceptions.Length);
            String json = BigWatson.Instance.ExportAsJsonAsync().Result;
            Assert.IsTrue(json.Length > 0);
            foreach (Exception exception in exceptions)
            {
                Assert.IsTrue(json.Contains(exception.GetType().Name));
                Assert.IsTrue(json.Contains(exception.Message));
                Assert.IsTrue(json.Contains(EventPriority.Info.ToString()));
                Assert.IsTrue(json.Contains(EventPriority.Warning.ToString()));
            }
        }
    }
}
