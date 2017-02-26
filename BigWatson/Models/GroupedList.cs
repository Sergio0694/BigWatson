using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace BigWatson.Models
{
    /// <summary>
    /// A class that represents a single group of data that can be displayed in a <see cref="Windows.UI.Xaml.Controls.SemanticZoom"/>
    /// </summary>
    /// <typeparam name="TKey">The type of the group key</typeparam>
    /// <typeparam name="TItems">The type of the items in the group</typeparam>
    internal sealed class GroupedList<TKey, TItems> : List<TItems>, IGrouping<TKey, TItems>
    {
        // Initializes a new instance with the input key and collection
        public GroupedList([NotNull] TKey key, [CanBeNull] IEnumerable<TItems> collection) : base(collection ?? new List<TItems>())
        {
            Key = key;
        }

        /// <summary>
        /// Key that represents the group of objects and used as group header.
        /// </summary>
        public TKey Key { get; }
    }
}
