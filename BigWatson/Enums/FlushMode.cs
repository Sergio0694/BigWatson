namespace BigWatsonDotNet.Enums
{
    /// <summary>
    /// Indicates the execution mode to use when flushing the saved logs
    /// </summary>
    public enum FlushMode : byte
    {
        /// <summary>
        /// The saved logs are flushed sequentially
        /// </summary>
        Serial,

        /// <summary>
        /// Saved logs are flushed in parallel (the input <see langword="delegate"/> must support parallel execution)
        /// </summary>
        Parallel
    }
}
