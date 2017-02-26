using System.Threading.Tasks;

namespace BigWatson.Shared.Misc
{
    /// <summary>
    /// A simple class with some useful extension methods
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Performs an explicit cast on the given object
        /// </summary>
        /// <typeparam name="T">The type to cast the object to</typeparam>
        /// <param name="item">The object to cast</param>
        public static T To<T>(this object item) => (T)item;

        /// <summary>
        /// Forgets a given task without raising any warnings
        /// </summary>
        /// <param name="task">The task to forget</param>
        public static void Forget(this Task task) { }
    }
}
