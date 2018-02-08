using System.Threading.Tasks;
using BigWatsonDotNet.Models.Abstract;

namespace BigWatsonDotNet.Delegates
{
    /// <summary>
    /// A <see langword="delegate"/> that uploads a log to a remote location
    /// </summary>
    /// <typeparam name="TLog">The type of log to upload</typeparam>
    /// <param name="log">The current log to upload</param>
    /// <returns><see langword="true"/> if the upload was completed successfully, <see langword="false"/> otherwise</returns>
    public delegate Task<bool> LogUploader<in TLog>(TLog log) where TLog : LogBase;
}
