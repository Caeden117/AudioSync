namespace AudioSync;

public sealed class SyncAnalyser
{
    private const int INTERVAL_DELTA = 1;
    private const int INTERVAL_DOWNSAMPLE = 5;

    private readonly double minimumBPM;
    private readonly double maximumBPM;
}
