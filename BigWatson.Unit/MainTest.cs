using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BigWatsonDotNet.Interfaces;
using BigWatsonDotNet.Models;
using BigWatsonDotNet.Models.Exceptions;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BigWatsonDotNet.Unit
{
    [TestClass]
    [TestCategory(nameof(MainTest))]
    public class MainTest
    {
        [TestMethod]
        public void LogTest()
        {
            // Log
            BigWatson.Instance.Reset();
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
        public void MemoryParserTest()
        {
            // Log
            BigWatson.Instance.Reset();
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
            BigWatson.Instance.Reset();
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
            BigWatson.Instance.Reset();
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

            // Checks
            LogsCollection<ExceptionReport> reports = BigWatson.Instance.LoadExceptionsAsync().Result;
            Assert.IsTrue(reports.LogsCount == exceptions.Length);
            String json = BigWatson.Instance.ExportAsJsonAsync().Result;
            Assert.IsTrue(json.Length > 0);
            foreach (Exception exception in exceptions)
            {
                Assert.IsTrue(json.Contains(exception.GetType().Name));
                Assert.IsTrue(json.Contains(exception.Message));
            }
        }
    }
}
