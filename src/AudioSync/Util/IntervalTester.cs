using MathNet.Numerics;

namespace AudioSync.Util;

internal sealed class IntervalTester
{
    public double[] Fitness => fitness;

    private readonly int minInterval;
    private readonly int maxInterval;
    private readonly int numIntervals;

    private readonly int sampleRate;

    private readonly double[] fitness;

    public IntervalTester(int sampleRate, double minBPM, double maxBPM)
    {
        this.sampleRate = sampleRate;

        minInterval = (int)((sampleRate * 60 / maxBPM) + 0.5);
        maxInterval = (int)((sampleRate * 60 / minBPM) + 0.5);
        numIntervals = maxInterval - minInterval;

        fitness = new double[numIntervals];
    }

    /// <summary>
    /// Converts a given interval to a BPM
    /// </summary>
    public double IntervalToBPM(int i) => sampleRate * 60.0 / (i + minInterval);

    /// <summary>
    /// Calculates fitness values with coarse intervals
    /// </summary>
    public void FillCoarseIntervals(GapData gapData)
    {
        const int intervalDelta = SyncAnalyser.INTERVAL_DELTA;
        
        var coarseIntervals = (numIntervals + intervalDelta - 1) / intervalDelta;

        Parallel.For(0, coarseIntervals, i =>
        {
            var idx = i * intervalDelta;
            var interval = minInterval + idx;
            fitness[i] = Math.Max(0.001, gapData.GetConfidenceForInterval(interval));
        });
    }

    /// <summary>
    /// Calculates fitness values over a range of intervals
    /// </summary>
    public (int begin, int end) FillIntervalRange(GapData gapData, Polynomial polyFit, int begin, int end)
    {
        begin = Math.Max(begin, 0);
        end = Math.Min(end, numIntervals);

        Parallel.For(begin, end, i =>
        {
            if (fitness[i] > 0) return;

            var interval = minInterval + begin;
            var confidence = gapData.GetConfidenceForInterval(interval) - polyFit.Evaluate(interval);

            fitness[i] = Math.Max(confidence, 0.1);
        });

        return (begin, end);
    }

    /// <summary>
    /// Given our calculated fitness values from <see cref="FillCoarseIntervals(GapData)"/> or <see cref="FillIntervalRange(GapData, Polynomial, int, int)"/>,
    /// return the best interval.
    /// </summary>
    public int FindBestInterval(double[] fitness, int begin, int end)
    {
        var best = 0;
        var highest = 0.0;

        Parallel.For(begin, end, i =>
        {
            if (fitness[i] > highest)
            {
                Interlocked.Exchange(ref highest, fitness[i]);
                Interlocked.Exchange(ref best, i);
            }
        });

        return best;
    }
}
