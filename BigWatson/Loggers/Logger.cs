﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BigWatsonDotNet.Enums;
using BigWatsonDotNet.Interfaces;
using BigWatsonDotNet.Models;
using BigWatsonDotNet.Models.Abstract;
using BigWatsonDotNet.Models.Realm;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Realms;

namespace BigWatsonDotNet.Loggers
{
    /// <summary>
    /// A complete exceptions manager with read and write permission
    /// </summary>
    internal sealed class Logger : ReadOnlyLogger, ILogger
    {
        public Logger([NotNull] RealmConfiguration configuration) : base(configuration) { }

        #region Log APIs

        /// <inheritdoc/>
        public void Log(Exception e)
        {
            RealmExceptionReport report = new RealmExceptionReport
            {
                Uid = Guid.NewGuid().ToString(),
                ExceptionType = e.GetType().ToString(),
                HResult = e.HResult,
                Message = e.Message,
                NativeStackTrace = e.StackTrace,
                StackTrace = e.Demystify().StackTrace,
                AppVersion = BigWatson.CurrentAppVersion.ToString(),
                UsedMemory = BigWatson.UsedMemoryParser(),
                Timestamp = DateTimeOffset.Now
            };
            Log(report);
        }

        /// <inheritdoc/>
        public void Log(EventPriority priority, String message)
        {
            RealmEvent report = new RealmEvent
            {
                Uid = Guid.NewGuid().ToString(),
                Priority = priority,
                Message = message,
                AppVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                Timestamp = DateTimeOffset.Now
            };
            Log(report);
        }

        // Inserts a single item in the local database
        private void Log([NotNull] RealmObject item)
        {
            try
            {
                using (Realm realm = Realm.GetInstance(Configuration))
                using (Transaction transaction = realm.BeginWrite())
                {
                    realm.Add(item);
                    transaction.Commit();
                }
            }
            catch
            {
                // This must never crash, or it'd invalidate other analytics tools
            }
        }

        #endregion

        #region Trim APIs

        /// <inheritdoc/>
        public Task TrimAsync(TimeSpan threshold) => TrimAsync(entry => DateTime.Now.Subtract(entry.Timestamp.DateTime) > threshold);

        /// <inheritdoc/>
        public Task TrimAsync(Version version) => TrimAsync(entry => Version.Parse(entry.AppVersion).CompareTo(version) < 0);

        // Trims the logs according to the given filter
        private Task TrimAsync([NotNull] Predicate<RealmExceptionReport> predicate)
        {
            return Task.Run(() =>
            {
                using (Realm realm = Realm.GetInstance(Configuration))
                using (Transaction transaction = realm.BeginWrite())
                {
                    foreach (RealmExceptionReport old in
                        from entry in realm.All<RealmExceptionReport>().ToArray()
                        where predicate(entry)
                        select entry)
                    {
                        realm.Remove(old);
                    }
                    transaction.Commit();
                }

                Realm.Compact(Configuration);
            });
        }

        /// <inheritdoc/>
        public Task TrimAsync<TLog>(TimeSpan threshold) where TLog : LogBase
        {
            return Task.Run(() =>
            {
                using (Realm realm = Realm.GetInstance(Configuration))
                using (Transaction transaction = realm.BeginWrite())
                {
                    // Execute the query
                    IEnumerable<RealmObject> query;
                    if (typeof(TLog) == typeof(Event))
                        query =
                            from entry in realm.All<RealmEvent>().ToArray()
                            where DateTime.Now.Subtract(entry.Timestamp.DateTime) > threshold
                            select entry;
                    else if (typeof(TLog) == typeof(ExceptionReport))
                        query =
                            from entry in realm.All<RealmExceptionReport>().ToArray()
                            where DateTime.Now.Subtract(entry.Timestamp.DateTime) > threshold
                            select entry;
                    else throw new ArgumentException("The input type is not valid", nameof(TLog));

                    // Trim the database
                    foreach (RealmObject item in query)
                    {
                        realm.Remove(item);
                    }
                    transaction.Commit();
                }

                Realm.Compact(Configuration);
            });
        }

        /// <inheritdoc/>
        public Task TrimAsync<TLog>(Version version) where TLog : LogBase
        {
            return Task.Run(() =>
            {
                using (Realm realm = Realm.GetInstance(Configuration))
                using (Transaction transaction = realm.BeginWrite())
                {
                    // Execute the query
                    IEnumerable<RealmObject> query;
                    if (typeof(TLog) == typeof(Event))
                        query =
                            from entry in realm.All<RealmEvent>().ToArray()
                            where Version.Parse(entry.AppVersion).CompareTo(version) < 0
                            select entry;
                    else if (typeof(TLog) == typeof(ExceptionReport))
                        query =
                            from entry in realm.All<RealmExceptionReport>().ToArray()
                            where Version.Parse(entry.AppVersion).CompareTo(version) < 0
                            select entry;
                    else throw new ArgumentException("The input type is not valid", nameof(TLog));

                    // Trim the database
                    foreach (RealmObject item in query)
                    {
                        realm.Remove(item);
                    }
                    transaction.Commit();
                }

                Realm.Compact(Configuration);
            });
        }

        #endregion

        #region Reset APIs

        /// <inheritdoc/>
        public Task ResetAsync()
        {
            return Task.Run(() =>
            {
                using (Realm realm = Realm.GetInstance(Configuration))
                using (Transaction transaction = realm.BeginWrite())
                {
                    realm.RemoveRange(realm.All<RealmEvent>());
                    realm.RemoveRange(realm.All<RealmExceptionReport>());
                    transaction.Commit();
                }

                Realm.Compact(Configuration);
            });
        }

        /// <inheritdoc/>
        public Task ResetAsync(Version version)
        {
            return Task.Run(() =>
            {
                using (Realm realm = Realm.GetInstance(Configuration))
                using (Transaction transaction = realm.BeginWrite())
                {
                    realm.RemoveRange(realm.All<RealmEvent>().Where(entry => entry.AppVersion == version.ToString()));
                    realm.RemoveRange(realm.All<RealmExceptionReport>().Where(entry => entry.AppVersion == version.ToString()));
                    transaction.Commit();
                }

                Realm.Compact(Configuration);
            });
        }

        /// <inheritdoc/>
        public Task ResetAsync<TLog>() where TLog : LogBase
        {
            return Task.Run(() =>
            {
                using (Realm realm = Realm.GetInstance(Configuration))
                using (Transaction transaction = realm.BeginWrite())
                {
                    // Execute the query
                    IQueryable<RealmObject> query;
                    if (typeof(TLog) == typeof(Event)) query = realm.All<RealmEvent>();
                    else if (typeof(TLog) == typeof(ExceptionReport)) query = realm.All<RealmExceptionReport>();
                    else throw new ArgumentException("The input type is not valid", nameof(TLog));

                    // Delete the items
                    realm.RemoveRange(query);
                    transaction.Commit();
                }

                Realm.Compact(Configuration);
            });
        }

        /// <inheritdoc/>
        public Task ResetAsync<TLog>(Version version) where TLog : LogBase
        {
            return Task.Run(() =>
            {
                using (Realm realm = Realm.GetInstance(Configuration))
                using (Transaction transaction = realm.BeginWrite())
                {
                    // Execute the query
                    IQueryable<RealmObject> query;
                    if (typeof(TLog) == typeof(Event)) query = realm.All<RealmEvent>().Where(entry => entry.AppVersion == version.ToString());
                    else if (typeof(TLog) == typeof(ExceptionReport)) query = realm.All<RealmExceptionReport>().Where(entry => entry.AppVersion == version.ToString());
                    else throw new ArgumentException("The input type is not valid", nameof(TLog));

                    // Delete the items
                    realm.RemoveRange(query);
                    transaction.Commit();
                }

                Realm.Compact(Configuration);
            });
        }

        #endregion

        #region File export

        /// <inheritdoc/>
        public Task<Stream> ExportAsync()
        {
            return Task.Run(() =>
            {
                // Copy the current database
                Stream stream = new MemoryStream();
                using (FileStream file = File.OpenRead(Configuration.DatabasePath))
                {
                    file.CopyTo(stream);
                }

                // Seek the result stream back to the start
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            });
        }

        /// <inheritdoc/>
        public Task ExportAsync(String path)
        {
            return Task.Run(() =>
            {
                using (FileStream
                    source = File.OpenRead(Configuration.DatabasePath),
                    destination = File.OpenWrite(path))
                {
                    source.CopyTo(destination);
                }
            });
        }

        #endregion

        #region JSON export

        /// <inheritdoc/>
        public Task<String> ExportAsJsonAsync() => ExportAsJsonAsync(TimeSpan.MaxValue, typeof(ExceptionReport), typeof(Event));

        /// <inheritdoc/>
        public Task<String> ExportAsJsonAsync(TimeSpan threshold) => ExportAsJsonAsync(threshold, typeof(ExceptionReport), typeof(Event));

        /// <inheritdoc/>
        public Task<String> ExportAsJsonAsync<TLog>() where TLog : LogBase => ExportAsJsonAsync(TimeSpan.MaxValue, typeof(TLog));

        /// <inheritdoc/>
        public Task<String> ExportAsJsonAsync<TLog>(TimeSpan threshold) where TLog : LogBase => ExportAsJsonAsync(threshold, typeof(TLog));

        /// <inheritdoc/>
        public async Task ExportAsJsonAsync(String path)
        {
            String json = await ExportAsJsonAsync();
            File.WriteAllText(path, json);
        }

        /// <inheritdoc/>
        public async Task ExportAsJsonAsync(String path, TimeSpan threshold)
        {
            String json = await ExportAsJsonAsync(threshold);
            File.WriteAllText(path, json);
        }

        /// <inheritdoc/>
        public async Task ExportAsJsonAsync<TLog>(String path) where TLog : LogBase
        {
            String json = await ExportAsJsonAsync<TLog>();
            File.WriteAllText(path, json);
        }

        /// <inheritdoc/>
        public async Task ExportAsJsonAsync<TLog>(String path, TimeSpan threshold) where TLog : LogBase
        {
            String json = await ExportAsJsonAsync<TLog>(threshold);
            File.WriteAllText(path, json);
        }

        // Writes the requested logs in JSON format
        [Pure, ItemNotNull]
        private async Task<String> ExportAsJsonAsync(TimeSpan threshold, [NotNull, ItemNotNull] params Type[] types)
        {
            // Checks
            if (types.Length < 1) throw new ArgumentException("The input types list can't be empty", nameof(types));
            if (types.Distinct().Count() != types.Length) throw new ArgumentException("The input types list can't contain duplicates", nameof(types));

            // Open the destination streams
            using (MemoryStream stream = new MemoryStream())
            using (StreamWriter writer = new StreamWriter(stream))
            using (JsonTextWriter jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented })
            {
                JObject jObj = await Task.Run(() =>
                {
                    JObject temp = new JObject();

                    // Prepare the logs
                    using (Realm realm = Realm.GetInstance(Configuration))
                    {
                        foreach (Type type in types)
                        {
                            if (type == typeof(ExceptionReport))
                            {
                                RealmExceptionReport[] exceptions = realm.All<RealmExceptionReport>().ToArray();
                                IList<JObject> jcrashes = (
                                    from exception in exceptions
                                    where DateTimeOffset.Now.Subtract(exception.Timestamp) < threshold
                                    orderby exception.Timestamp descending
                                    select JObject.FromObject(exception)).ToList();
                                temp["ExceptionsCount"] = jcrashes.Count;
                                temp["Exceptions"] = new JArray(jcrashes);
                            }
                            else if (type == typeof(Event))
                            {
                                RealmEvent[] events = realm.All<RealmEvent>().ToArray();
                                JsonSerializer converter = JsonSerializer.CreateDefault(new JsonSerializerSettings { Converters = new List<JsonConverter> { new StringEnumConverter() } });
                                IList<JObject> jevents = (
                                    from log in events
                                    where DateTimeOffset.Now.Subtract(log.Timestamp) < threshold
                                    orderby log.Timestamp descending
                                    select JObject.FromObject(log, converter)).ToList();
                                temp["EventsCount"] = jevents.Count;
                                temp["Events"] = new JArray(jevents);
                            }
                        }
                    }

                    return temp;
                });

                // Write the JSON data
                await jObj.WriteToAsync(jsonWriter);
                await jsonWriter.FlushAsync();

                // Return the JSON
                byte[] bytes = stream.ToArray();
                return Encoding.UTF8.GetString(bytes);
            }
        }

        #endregion
    }
}
