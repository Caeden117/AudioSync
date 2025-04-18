﻿namespace AudioSync;

/// <summary>
/// A result from sync analysis, providing confidence, BPM, and song offset.
/// </summary>
/// <param name="Fitness">
/// An arbitrary number, with no minimum or maximum, representing the algorithm's confidence in this result.
/// A higher <paramref name="Fitness"/> implies more confidence in this result.
/// </param>
/// <param name="BPM">
/// The detected BPM. Please note that, given minimum or maximum BPM parameters set during sync analysis, this BPM may be halved or doubled from the true BPM.
/// </param>
/// <param name="Offset">
/// Given the <paramref name="BPM"/>, this estimates the song offset in seconds.
/// </param>
public readonly record struct SyncResult(float Fitness, float BPM, float Offset)
{
    /// <summary>
    /// Length of one beat, in seconds
    /// </summary>
    public float Beat => 60f / BPM;
}
