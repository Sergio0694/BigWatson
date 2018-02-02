﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace BigWatsonDotNet.Models
{
    /// <summary>
    /// A class that represents a single group of data to display to the end user
    /// </summary>
    /// <typeparam name="TKey">The type of the group key</typeparam>
    /// <typeparam name="TValue">The type of the items in the group</typeparam>
    internal sealed class GroupedList<TKey, TValue> : IReadOnlyList<TValue>, IGrouping<TKey, TValue> 
        where TKey : class 
        where TValue : class
    {
        /// <inheritdoc/>
        [NotNull]
        public TKey Key { get; }

        // The underlying items collection
        [NotNull, ItemNotNull]
        private readonly IReadOnlyList<TValue> Items;

        public GroupedList([NotNull] TKey key, [CanBeNull, ItemNotNull] IReadOnlyList<TValue> items)
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
