using System.Diagnostics.CodeAnalysis;

namespace AudioSync.Util.Exceptions;

/// <summary>
/// An exception thrown by AudioSync, where the state of execution is unrecoverable and should never be retried with current parameters.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class AudioSyncFatalException : Exception
{
    public AudioSyncFatalException(string msg) : base(msg) { }
}
