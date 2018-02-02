using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace BigWatson.Models
{
    /// <summary>
    /// A class that represents a single group of data to display to the end user
    /// </summary>
    /// <typeparam name="TKey">The type of the group key</typeparam>
    /// <typeparam name="TItems">The type of the items in the group</typeparam>
    internal sealed class GroupedList<TKey, TItems> : List<TItems>, IGrouping<TKey, TItems> where TKey : class where TItems : class
    {
        // Initializes a new instance with the input key and collection
        public GroupedList([NotNull] TKey key, [CanBeNull, ItemNotNull] IEnumerable<TItems> collection) : base(collection ?? new List<TItems>())
        {
            Key = key;
        }

        /// <inheritdoc/>
        [NotNull]
        public TKey Key { get; }
    }
}
