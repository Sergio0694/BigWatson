using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BigWatsonDotNet.Interfaces;
using BigWatsonDotNet.Models;
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
            ExceptionsCollection reports = BigWatson.Instance.LoadCrashReportsAsync().Result;
            Assert.IsTrue(reports.ExceptionsCount == 1);
            Assert.IsTrue(reports.Exceptions.First().ExceptionType.Equals(typeof(InvalidOperationException).ToString()));
            Assert.IsTrue(reports.Exceptions.First().Message.Equals("Hello world!"));
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
            ExceptionsCollection reports = BigWatson.Instance.LoadCrashReportsAsync().Result;
            Assert.IsTrue(reports.Exceptions.First().UsedMemory == 128L);
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
            BigWatson.Instance.ExportDatabaseAsync(path).Wait();

            // Check
            IReadOnlyExceptionManager loaded = BigWatson.Load(path);
            ExceptionsCollection reports = loaded.LoadCrashReportsAsync().Result;
            Assert.IsTrue(reports.ExceptionsCount == 1);
            Assert.IsTrue(reports.Exceptions.First().ExceptionType.Equals(typeof(InvalidOperationException).ToString()));
            Assert.IsTrue(reports.Exceptions.First().Message.Equals("Export test"));
            File.Delete(path);
        }
    }
}