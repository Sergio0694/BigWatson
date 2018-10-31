using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BigWatsonDotNet.Enums;
using BigWatsonDotNet.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BigWatsonDotNet.Unit
{
    [TestClass]
    [TestCategory(nameof(CrashReportsTest))]
    public class CrashReportsTest
    {
        [TestMethod]
        public void AnyTest()
        {
            BigWatson.Instance.ResetAsync().Wait();
            bool any = BigWatson.Instance.AnyExceptionsAsync().Result;
            Assert.IsFalse(any);
            try
            {
                throw new InvalidOperationException("Hello world!");
            }
            catch (Exception e)
            {
                BigWatson.Instance.Log(e);
            }
            any = BigWatson.Instance.AnyExceptionsAsync().Result;
            Assert.IsTrue(any);
        }

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
            IReadOnlyCollection<ExceptionReport> reports = BigWatson.Instance.LoadExceptionsAsync(Assembly.GetEntryAssembly().GetName().Version).Result;
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

        [TestMethod]
        public void SequentialFlushTest()
        {
            // Log
            BigWatson.Instance.ResetAsync().Wait();
            Exception[] exceptions =
            {
                new ArgumentException("Hello world!"),
                new ArithmeticException("Division by zero"),
                new ArgumentException("We're being too lazy here!"),
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
            Assert.IsTrue(BigWatson.Instance.LoadExceptionsAsync().Result.LogsCount == 3);
            Assert.IsTrue(BigWatson.Instance.TryFlushAsync<ExceptionReport>(log => Task.Delay(500).ContinueWith(_ => true), CancellationToken.None).Result == 3);
            Assert.IsTrue(BigWatson.Instance.LoadExceptionsAsync().Result.LogsCount == 0);
        }

        [TestMethod]
        public void SequentialFlushFailTest()
        {
            // Log
            BigWatson.Instance.ResetAsync().Wait();
            Exception[] exceptions =
            {
                new ArgumentException("Hello world!"),
                new ArithmeticException("Division by zero"),
                new ArgumentException("We're being too lazy here!"),
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
            Assert.IsTrue(BigWatson.Instance.LoadExceptionsAsync().Result.LogsCount == 3);
            bool flushed = false;
            Assert.IsTrue(BigWatson.Instance.TryFlushAsync<ExceptionReport>(async log =>
            {
                if (flushed) return false;
                flushed = true;
                await Task.Delay(500);
                return true;
            }, CancellationToken.None).Result == 1);
            Assert.IsTrue(BigWatson.Instance.LoadExceptionsAsync().Result.LogsCount == 2);
        }

        [TestMethod]
        public void ParallelFlushTest()
        {
            // Log
            BigWatson.Instance.ResetAsync().Wait();
            Exception[] exceptions =
            {
                new ArgumentException("Hello world!"),
                new ArithmeticException("Division by zero"),
                new ArgumentException("We're being too lazy here!"),
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
            Assert.IsTrue(BigWatson.Instance.LoadExceptionsAsync().Result.LogsCount == 3);
            Assert.IsTrue(BigWatson.Instance.TryFlushAsync<ExceptionReport>(log => Task.Delay(500).ContinueWith(_ => true), CancellationToken.None, FlushMode.Parallel).Result == 3);
            Assert.IsTrue(BigWatson.Instance.LoadExceptionsAsync().Result.LogsCount == 0);
        }

        [TestMethod]
        public void ParallelFlushFailTest()
        {
            // Log
            BigWatson.Instance.ResetAsync().Wait();
            Exception[] exceptions =
            {
                new ArgumentException("Hello world!"),
                new ArithmeticException("Division by zero"),
                new ArgumentException("We're being too lazy here!"),
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
            Assert.IsTrue(BigWatson.Instance.LoadExceptionsAsync().Result.LogsCount == 3);
            int flushed = 0;
            Assert.IsTrue(BigWatson.Instance.TryFlushAsync<ExceptionReport>(async log =>
            {
                await Task.Delay(500);
                return Interlocked.Increment(ref flushed) <= 2;
            }, CancellationToken.None, FlushMode.Parallel).Result == 2);
            Assert.IsTrue(BigWatson.Instance.LoadExceptionsAsync().Result.LogsCount == 1);
        }
    }
}
