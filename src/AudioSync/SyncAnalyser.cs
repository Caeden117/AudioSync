using AudioSync.OnsetDetection;
using AudioSync.OnsetDetection.Structures;
using AudioSync.Structures;
using System.Collections.Generic;

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

    public Task<List<SyncResult>> RunAsync(double[] monoAudioData, int sampleRate, int blockSize = 2048, int hopSize = 256)
        => Task.Run(() => Run(monoAudioData, sampleRate, blockSize, hopSize));

    public List<SyncResult> Run(double[] monoAudioData, int sampleRate, int blockSize = 2048, int hopSize = 256)
    {
        var results = new List<SyncResult>();

        // From mattmora's testing, Complex Domain with 0.1 threshold seems to give best results
        var onsetDetection = new OnsetDetector(OnsetType.ComplexDomain, blockSize, hopSize, sampleRate)
        {
            Threshold = 0.1f
        };

        Span<double> hopData = stackalloc double[hopSize];
        var samples = monoAudioData.Length;
        var onsetOutput = 0.0;
        var detectedOnsets = new List<Onset>();

        // Find offsets in audio
        for (var i = 0; i < samples; i++)
        {
            hopData[i % hopSize] = monoAudioData[i];

            // Wait until we've filled up our hop data
            if (i % hopSize < hopSize - 1) continue;

            onsetDetection.Do(in hopData, ref onsetOutput);

            // If we do not find an onset, do not bother
            if (onsetOutput < 1) continue;

            // Calculate strength of newly detected onset by taking the average of samples around the onset position
            var position = onsetDetection.LastOffset;

            var windowMin = Math.Max(0, position - (STRENGTH_WINDOW_SIZE / 2));
            var windowMax = Math.Min(monoAudioData.Length, position + (STRENGTH_WINDOW_SIZE / 2));

            var strength = 0.0;
            for (var j = windowMin; j < windowMax; j++)
            {
                strength += Math.Abs(monoAudioData[j]);
            }
            strength /= Math.Max(1, windowMax - windowMin);

            detectedOnsets.Add(new(onsetDetection.LastOffset, strength));
        }

        // TODO: Calculate BPM

        // TODO: Calculate offsets

        return results;
    }
}
