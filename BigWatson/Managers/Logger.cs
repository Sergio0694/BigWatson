using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BigWatsonDotNet.Enums;
using BigWatsonDotNet.Interfaces;
using BigWatsonDotNet.Models.Events;
using BigWatsonDotNet.Models.Exceptions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Realms;

namespace BigWatsonDotNet.Managers
{
    /// <summary>
    /// A complete exceptions manager with read and write permission
    /// </summary>
    internal sealed class Logger : ReadOnlyLogger, ILogger
    {
        public Logger([NotNull] RealmConfiguration configuration) : base(configuration) { }

        #region Write APIs

        /// <inheritdoc/>
        public void Log(Exception e)
        {
            RealmExceptionReport report = new RealmExceptionReport
            {
                Uid = Guid.NewGuid().ToString(),
                ExceptionType = e.GetType().ToString(),
                HResult = e.HResult,
                Message = e.Message,
                StackTrace = e.StackTrace,
                AppVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
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
                Priority = (byte)priority,
                Message = message,
                AppVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                Timestamp = DateTimeOffset.Now
            };
            Log(report);
        }

        // Inserts a single item in the local database
        private void Log([NotNull] RealmObject item)
        {
            using (Realm realm = Realm.GetInstance(Configuration))
            using (Transaction transaction = realm.BeginWrite())
            {
                realm.Add(item);
                transaction.Commit();
            }
        }

        /// <inheritdoc/>
        public void Reset() => Realm.DeleteRealm(Configuration);

        /// <inheritdoc/>
        public async Task TrimAsync(TimeSpan threshold)
        {
            using (Realm realm = await Realm.GetInstanceAsync(Configuration))
            {
                foreach (RealmExceptionReport old in
                    from entry in realm.All<RealmExceptionReport>().ToArray()
                    where DateTime.Now.Subtract(entry.Timestamp.DateTime) > threshold
                    select entry)
                {
                    realm.Remove(old);
                }
            }

            Realm.Compact();
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
                    byte[] buffer = new byte[8192];
                    while (true)
                    {
                        int read = file.Read(buffer, 0, buffer.Length);
                        if (read > 0) stream.Write(buffer, 0, read);
                        else break;
                    }
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
                    byte[] buffer = new byte[8192];
                    while (true)
                    {
                        int read = source.Read(buffer, 0, buffer.Length);
                        if (read > 0) destination.Write(buffer, 0, read);
                        else break;
                    }
                }
            });
        }

        #endregion

        #region JSON export

        /// <inheritdoc/>
        public async Task<String> ExportAsJsonAsync()
        {
            using (MemoryStream stream = new MemoryStream())
            using (StreamWriter writer = new StreamWriter(stream))
            using (JsonTextWriter jsonWriter = new JsonTextWriter(writer) { Formatting = Formatting.Indented })
            using (Realm realm = await Realm.GetInstanceAsync(Configuration))
            {
                // Prepare the logs
                RealmExceptionReport[] exceptions = realm.All<RealmExceptionReport>().ToArray();
                IList<JObject> jcrashes =
                    (from exception in exceptions
                     orderby exception.Timestamp descending
                     select JObject.FromObject(exception)).ToList();
                RealmEvent[] events = realm.All<RealmEvent>().ToArray();
                IList<JObject> jevents =
                    (from log in events
                     orderby log.Timestamp descending
                     select JObject.FromObject(log)).ToList();

                // Write the JSON data
                JObject jObj = new JObject
                {
                    ["Count"] = exceptions.Length,
                    ["Exceptions"] = new JArray(jcrashes),
                    ["Events"] = new JArray(jevents)
                };
                await jObj.WriteToAsync(jsonWriter);
                await jsonWriter.FlushAsync();

                // Return the JSON
                byte[] bytes = stream.ToArray();
                return Encoding.UTF8.GetString(bytes);
            }
        }

        /// <inheritdoc/>
        public async Task ExportAsJsonAsync(String path)
        {
            String json = await ExportAsJsonAsync();
            File.WriteAllText(path, json);
        }

        #endregion
    }
}
