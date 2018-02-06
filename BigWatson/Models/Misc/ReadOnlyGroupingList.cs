using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace BigWatsonDotNet.Models.Misc
{
    /// <summary>
    /// A class that represents a group of items with shared key
    /// </summary>
    /// <typeparam name="TKey">The type of the group key</typeparam>
    /// <typeparam name="TValue">The type of the items in the group</typeparam>
    public sealed class ReadOnlyGroupingList<TKey, TValue> : IReadOnlyList<TValue>, IGrouping<TKey, TValue>
        where TKey : class 
        where TValue : class
    {
        /// <inheritdoc/>
        [NotNull]
        public TKey Key { get; }

        // The underlying items collection
        [NotNull, ItemNotNull]
        private readonly IReadOnlyList<TValue> Items;

        internal ReadOnlyGroupingList([NotNull] TKey key, [CanBeNull, ItemNotNull] IReadOnlyList<TValue> items)
        {
            Key = key;
            Items = items ?? new TValue[0];
        }

        #region Interface

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public IEnumerator<TValue> GetEnumerator() => Items.GetEnumerator();

        /// <inheritdoc/>
        public int Count => Items.Count;

        /// <inheritdoc/>
        public TValue this[int index] => Items[index];

        #endregion
    }
}
