using JetBrains.Annotations;

namespace BigWatsonDotNet.Enums
{
    /// <summary>
    /// Indicates a priority level for a logged event
    /// </summary>
    [PublicAPI]
    public enum EventPriority : byte
    {
        /// <summary>
        /// The maximum priority, the event indicates an error occurred in the app
        /// </summary>
        Error = 0,

        /// <summary>
        /// A warning event that should be investigated
        /// </summary>
        Warning = 1,

        /// <summary>
        /// An info event, the first priority level not directly associated with potentially dangerous situations
        /// </summary>
        Info = 2,

        /// <summary>
        /// An extended info event, with verbose details that could generally be ignored
        /// </summary>
        Verbose = 3,

        /// <summary>
        /// A debug event, that can be used to investigate problems or implementations
        /// </summary>
        Debug = 4
    }
}