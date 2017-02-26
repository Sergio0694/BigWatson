using System;
using JetBrains.Annotations;

namespace BigWatson.Models
{
    /// <summary>
    /// A simple model that wraps a value and a label
    /// </summary>
    public sealed class ChartData
    {
        /// <summary>
        /// Gets the value of this instance
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Gets the label associated with the current value
        /// </summary>
        [NotNull]
        public String Label { get; }

        // Internal constructor
        internal ChartData(int value, [NotNull] String label)
        {
            Value = value;
            Label = label;
        }
    }
}
