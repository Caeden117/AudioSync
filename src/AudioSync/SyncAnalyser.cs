namespace AudioSync;

public sealed class SyncAnalyser
{
    private const int INTERVAL_DELTA = 1;
    private const int INTERVAL_DOWNSAMPLE = 5;

    // Size of window around onset sample used to calculate onset strength, can significantly affect results
    // 200 is from the original code, not sure where it comes from, but it seems to work well
    private const int STRENGTH_WINDOW_SIZE = 200;

    // Fitness ratio of rounded to un-rounded required to accept a rounded BPM
    private const double ROUNDING_THRESHOLD = 0.95;

    private readonly double minimumBPM;
    private readonly double maximumBPM;

    public SyncAnalyser(double minimumBPM, double maximumBPM)
    {
        this.minimumBPM = minimumBPM;
        this.maximumBPM = maximumBPM;
    }

    public Task<List<SyncResult>> RunAsync(float[] audioData, int channels, int sampleRate, int blockSize = 2048, int hopSize = 256)
        => Task.Run(() => Run(audioData, channels, sampleRate, blockSize, hopSize));

    public List<SyncResult> Run(float[] audioData, int channels, int sampleRate, int blockSize = 2048, int hopSize = 256)
    {
        var results = new List<SyncResult>();



        return results;
    }
}
