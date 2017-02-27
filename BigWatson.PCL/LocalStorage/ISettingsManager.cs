using System;
using JetBrains.Annotations;

namespace BigWatson.PCL.Helpers
{
    /// <summary>
    /// An interface to manage cross-platform settings
    /// </summary>
    public interface ISettingsManager
    {
        /// <summary>
        /// Checks whether or not there is a saved setting with the given key
        /// </summary>
        /// <param name="key">The key to look for</param>
        bool ContainsKey([NotNull] String key);

        /// <summary>
        /// Adds or updates a setting value
        /// </summary>
        /// <typeparam name="T">The type of the setting to store</typeparam>
        /// <param name="key">The key of the setting</param>
        /// <param name="value">The value of the setting to store</param>
        void AddOrUpdateValue<T>([NotNull] String key, T value);

        /// <summary>
        /// Tries to get a setting value, returns the default value if it's not present
        /// </summary>
        /// <typeparam name="T">The type of the setting to get</typeparam>
        /// <param name="key">The key to use to retrieve the setting</param>
        T GetValueOrDefault<T>([NotNull] String key);

        /// <summary>
        /// Clears all the existing settings
        /// </summary>
        void Clear();
    }
}