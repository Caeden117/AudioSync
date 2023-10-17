using System.Diagnostics.CodeAnalysis;

namespace AudioSync.Util.Exceptions;

/// <summary>
/// An exception thrown by AudioSync, where the throwing method(s) can be retried with potentially better results.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class AudioSyncRetryableException : Exception
{
    public AudioSyncRetryableException(string msg) : base(msg) { }
}
