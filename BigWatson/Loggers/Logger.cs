﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BigWatsonDotNet.Delegates;
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
                AppVersion = (BigWatson.VersionParser?.Invoke() ??
                              (Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly()).GetName().Version).ToString(),
                UsedMemory = BigWatson.MemoryParser?.Invoke() ?? Process.GetCurrentProcess().PrivateMemorySize64,
                Timestamp = DateTimeOffset.Now
            };
            Log(report);
        }

        /// <inheritdoc/>
        public void Log(EventPriority priority, string message)
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

        // Trims the logs according to the given predicate
        private Task TrimAsync([NotNull] Predicate<ILog> predicate)
        {
            return Task.Run(() =>
            {
                using (Realm realm = Realm.GetInstance(Configuration))
                using (Transaction transaction = realm.BeginWrite())
                {
                    foreach (RealmObject old in new IEnumerable<RealmObject>[]
                    {
                        realm.All<RealmExceptionReport>().ToArray().Where(log => predicate(log)),
                        realm.All<RealmEvent>().ToArray().Where(log => predicate(log))
                    }.SelectMany(l => l))
                    {
                        realm.Remove(old);
                    }
                    transaction.Commit();
                }

                Realm.Compact(Configuration);
            });
        }

        /// <inheritdoc/>
        public Task TrimAsync<TLog>(TimeSpan threshold) where TLog : LogBase => TrimAsync<TLog>(entry => DateTime.Now.Subtract(entry.Timestamp.DateTime) > threshold);

        /// <inheritdoc/>
        public Task TrimAsync<TLog>(Version version) where TLog : LogBase => TrimAsync<TLog>(entry => Version.Parse(entry.AppVersion).CompareTo(version) < 0);

        // Trims the saved logs according to the input predicate
        private Task TrimAsync<TLog>([NotNull] Predicate<ILog> predicate) where TLog : LogBase
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
                            where predicate(entry)
                            select entry;
                    else if (typeof(TLog) == typeof(ExceptionReport))
                        query =
                            from entry in realm.All<RealmExceptionReport>().ToArray()
                            where predicate(entry)
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
        public Task ResetAsync<TLog>(Predicate<TLog> predicate) where TLog : LogBase
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task ResetAsync(Version version)
        {
            return Task.Run(() =>
            {
                using (Realm realm = Realm.GetInstance(Configuration))
                using (Transaction transaction = realm.BeginWrite())
                {
                    string _version = version.ToString();
                    realm.RemoveRange(realm.All<RealmEvent>().Where(entry => entry.AppVersion == _version));
                    realm.RemoveRange(realm.All<RealmExceptionReport>().Where(entry => entry.AppVersion == _version));
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
                    string _version = version.ToString();
                    IQueryable<RealmObject> query;
                    if (typeof(TLog) == typeof(Event)) query = realm.All<RealmEvent>().Where(entry => entry.AppVersion == _version);
                    else if (typeof(TLog) == typeof(ExceptionReport)) query = realm.All<RealmExceptionReport>().Where(entry => entry.AppVersion == _version);
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
        public Task ExportAsync(string path)
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
        public Task<string> ExportAsJsonAsync() => ExportAsJsonAsync(null, typeof(ExceptionReport), typeof(Event));

        /// <inheritdoc/>
        public Task<string> ExportAsJsonAsync<TLog>(Predicate<TLog> predicate) where TLog : LogBase
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task<string> ExportAsJsonAsync(TimeSpan threshold) => ExportAsJsonAsync(entry => DateTimeOffset.Now.Subtract(entry.Timestamp) < threshold, typeof(ExceptionReport), typeof(Event));

        /// <inheritdoc/>
        public Task<string> ExportAsJsonAsync(Version version) => ExportAsJsonAsync(entry => entry.AppVersion == version.ToString(), typeof(ExceptionReport), typeof(Event));

        /// <inheritdoc/>
        public Task<string> ExportAsJsonAsync<TLog>() where TLog : LogBase => ExportAsJsonAsync(null, typeof(TLog));

        /// <inheritdoc/>
        public Task<string> ExportAsJsonAsync<TLog>(TimeSpan threshold) where TLog : LogBase => ExportAsJsonAsync(entry => DateTimeOffset.Now.Subtract(entry.Timestamp) < threshold, typeof(TLog));

        /// <inheritdoc/>
        public Task<string> ExportAsJsonAsync<TLog>(Version version) where TLog : LogBase => ExportAsJsonAsync(entry => entry.AppVersion == version.ToString(), typeof(TLog));

        /// <inheritdoc/>
        public async Task ExportAsJsonAsync(string path)
        {
            string json = await ExportAsJsonAsync();
            File.WriteAllText(path, json);
        }

        /// <inheritdoc/>
        public Task ExportAsJsonAsync<TLog>(string path, Predicate<TLog> predicate) where TLog : LogBase
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public async Task ExportAsJsonAsync(string path, TimeSpan threshold)
        {
            string json = await ExportAsJsonAsync(threshold);
            File.WriteAllText(path, json);
        }

        /// <inheritdoc/>
        public async Task ExportAsJsonAsync(string path, Version version)
        {
            string json = await ExportAsJsonAsync(version);
            File.WriteAllText(path, json);
        }

        /// <inheritdoc/>
        public async Task ExportAsJsonAsync<TLog>(string path) where TLog : LogBase
        {
            string json = await ExportAsJsonAsync<TLog>();
            File.WriteAllText(path, json);
        }

        /// <inheritdoc/>
        public async Task ExportAsJsonAsync<TLog>(string path, TimeSpan threshold) where TLog : LogBase
        {
            string json = await ExportAsJsonAsync<TLog>(threshold);
            File.WriteAllText(path, json);
        }

        /// <inheritdoc/>
        public async Task ExportAsJsonAsync<TLog>(string path, Version version) where TLog : LogBase
        {
            string json = await ExportAsJsonAsync<TLog>(version);
            File.WriteAllText(path, json);
        }

        // Writes the requested logs in JSON format
        [Pure, ItemNotNull]
        private async Task<string> ExportAsJsonAsync([CanBeNull] Predicate<ILog> predicate, [NotNull, ItemNotNull] params Type[] types)
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
                                    where predicate?.Invoke(exception) ?? true
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
                                    where predicate?.Invoke(log) ?? true
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

        #region Flush

        /// <inheritdoc/>
        public Task<int> TryFlushAsync<TLog>(LogUploader<TLog> uploader, CancellationToken token) where TLog : LogBase
            => TryFlushAsync(uploader, token, FlushMode.Serial);

        /// <inheritdoc/>
        public Task<int> TryFlushAsync<TLog>(LogUploaderWithToken<TLog> uploader, CancellationToken token) where TLog : LogBase
            => TryFlushAsync(uploader, token, FlushMode.Serial);

        /// <inheritdoc/>
        public Task<int> TryFlushAsync<TLog>(LogUploader<TLog> uploader, CancellationToken token, FlushMode mode) where TLog : LogBase
            => TryFlushAsync<TLog>((log, _) => uploader(log), token, mode);

        /// <inheritdoc/>
        public Task<int> TryFlushAsync<TLog>(LogUploaderWithToken<TLog> uploader, CancellationToken token, FlushMode mode) where TLog : LogBase
        {
            if (typeof(TLog) == typeof(ExceptionReport)) return TryFlushAsync<TLog, RealmExceptionReport>(uploader, token, mode);
            if (typeof(TLog) == typeof(Event)) return TryFlushAsync<TLog, RealmEvent>(uploader, token, mode);
            throw new ArgumentException("The input type is not valid", nameof(TLog));
        }

        // Flush implementation
        private async Task<int> TryFlushAsync<TLog, TRealm>(LogUploaderWithToken<TLog> uploader, CancellationToken token, FlushMode mode) 
            where TLog : LogBase
            where TRealm : RealmObject, ILog
        {
            // Map all the existing logs to public type instances and Uid pairs
            (TLog Log, string Uid)[] query = await Task.Run(() =>
            {
                using (Realm realm = Realm.GetInstance(Configuration))
                {
                    if (typeof(TLog) == typeof(ExceptionReport))
                    {
                        RealmExceptionReport[] data = realm.All<RealmExceptionReport>().ToArray();

                        return (
                            from raw in data
                            let sameType = (
                                from item in data
                                where item.ExceptionType.Equals(raw.ExceptionType)
                                select item).ToArray()
                            let versions = (
                                from entry in sameType
                                group entry by entry.AppVersion
                                into version
                                select version.Key).ToArray()
                            let item = new ExceptionReport(raw,
                                versions[0], versions[versions.Length - 1], sameType.Length,
                                sameType[0].Timestamp, sameType[sameType.Length - 1].Timestamp)
                            select ((TLog)(object)item, raw.Uid)).ToArray();
                    }

                    if (typeof(TLog) == typeof(Event))
                    {
                        return (
                            from raw in realm.All<RealmEvent>().ToArray()
                            let item = new Event(raw)
                            select ((TLog)(object)item, raw.Uid)).ToArray();
                    }

                    throw new ArgumentException("The input type is not valid", nameof(TLog));
                }
            });

            // Local function to remove a saved log with a specified Uid
            void RemoveUid(string uid)
            {
                using (Realm realm = Realm.GetInstance(Configuration))
                {
                    TRealm target = realm.All<TRealm>().First(row => row.Uid == uid);
                    using (Transaction transaction = realm.BeginWrite())
                    {
                        realm.Remove(target);
                        transaction.Commit();
                    }
                }
            }
            
            // Flush the items
            int flushed = 0;
            switch (mode)
            {
                // Sequentially upload and remove the logs, stop as soon as one fails
                case FlushMode.Serial:
                    foreach (var pair in query)
                    {
                        if (token.IsCancellationRequested || !await uploader(pair.Log, token)) break;
                        RemoveUid(pair.Uid);
                        flushed++;
                    }
                    return flushed;

                // Upload the logs in parallel, then remove the ones uploaded correctly
                case FlushMode.Parallel:
                    bool[] results = await Task.WhenAll(query.Select(pair => uploader(pair.Log, token)));
                    for (int i = 0; i < results.Length; i++)
                        if (results[i])
                        {
                            RemoveUid(query[i].Uid);
                            flushed++;
                        }
                    return flushed;
                default: throw new ArgumentException("The input flush mode is not valid", nameof(mode));
            }
        }

        #endregion
    }
}
