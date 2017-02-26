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
    public sealed class JumpListGroup<TKey, TItems> : List<TItems>
    {
        internal JumpListGroup([NotNull] TKey key, [CanBeNull] IEnumerable<TItems> collection) : base(collection ?? new List<TItems>())
        {
            Key = key;
        }

        internal JumpListGroup([NotNull] IGrouping<TKey, TItems> collection) : base(collection)
        {
            Key = collection.Key;
        }

        /// <summary>
        /// Key that represents the group of objects and used as group header.
        /// </summary>
        public TKey Key { get; }
    }
}
