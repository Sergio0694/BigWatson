using System.Threading;
using System.Threading.Tasks;
using BigWatsonDotNet.Models.Abstract;

namespace BigWatsonDotNet.Delegates
{
    /// <summary>
    /// A <see langword="delegate"/> that uploads a log to a remote location
    /// </summary>
    /// <typeparam name="TLog">The type of log to upload</typeparam>
    /// <param name="log">The current log to upload</param>
    /// <param name="token">A <see cref="CancellationToken"/> for the upload operation</param>
    /// <returns><see langword="true"/> if the upload was completed successfully, <see langword="false"/> otherwise</returns>
    public delegate Task<bool> LogUploaderWithToken<in TLog>(TLog log, CancellationToken token) where TLog : LogBase;
}
