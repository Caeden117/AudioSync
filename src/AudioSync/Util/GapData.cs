using AudioSync.Structures;
using MathNet.Numerics;

namespace AudioSync.Util;

// Gap confidence evaluation
internal sealed class GapData
{
    private readonly List<Onset> onsets;
    private readonly int[] wrappedPos;
    private readonly double[] wrappedOnsets;
    private readonly double[] hammingWindow;
    private readonly int downsample;
    private readonly int onsetCount;

    public GapData(int bufferSize, int downsample, List<Onset> onsets)
    {
        this.onsets = onsets;
        onsetCount = onsets.Count;

        this.downsample = downsample;
        hammingWindow = CreateHammingWindow(2048 >> downsample);

        wrappedPos = new int[onsetCount];
        wrappedOnsets = new double[bufferSize];
    }

    /// <summary>
    /// Returns a confidence value representing the vicinity of nearby onsets
    /// </summary>
    public double GapConfidence(int gapPos, int interval)
    {
        var windowSize = hammingWindow.Length;
        var halfWindowSize = windowSize / 2;
        var area = 0.0;

        var beginOnset = gapPos - halfWindowSize;
        var endOnset = gapPos + halfWindowSize;

        if (beginOnset < 0)
        {
            var wrappedBegin = beginOnset + interval;
            for (var i = wrappedBegin; i < interval; ++i)
            {
                var windowIndex = i - wrappedBegin;
                area += wrappedOnsets[i] * hammingWindow[windowIndex];
            }
            beginOnset = 0;
        }

        if (endOnset > interval)
        {
            var wrappedEnd = endOnset - interval;
            var indexOffset = windowSize - wrappedEnd;
            for (var i = 0; i < wrappedEnd; ++i)
            {
                var windowIndex = i + indexOffset;
                area += wrappedOnsets[i] * hammingWindow[windowIndex];
            }
            endOnset = interval;
        }

        for (var i = beginOnset; i < endOnset; ++i)
        {
            var windowIndex = i - beginOnset;
            area += wrappedOnsets[i] * hammingWindow[windowIndex];
        }

        return area;
    }

    /// <summary>
    /// Returns the gap confidence for the specified interval
    /// </summary>
    public double GetConfidenceForInterval(int interval)
    {
        Array.Clear(wrappedOnsets, 0, wrappedOnsets.Length);

        // Make a histogram of onset strengths for every position in the interval.
        var reducedInterval = interval >> downsample;
        for (var i = 0; i < onsetCount; ++i)
        {
            var pos = (onsets[i].Position % interval) >> downsample;
            wrappedPos[i] = pos;
            wrappedOnsets[pos] += onsets[i].Strength;
        }

        // Record the amount of support for each gap value.
        var highestConfidence = 0.0;
        for (var i = 0; i < onsetCount; ++i)
        {
            var pos = wrappedPos[i];
            var confidence = GapConfidence(pos, reducedInterval);
            var offbeatPos = (pos + (reducedInterval / 2)) % reducedInterval;
            confidence += GapConfidence(offbeatPos, reducedInterval) * 0.5;

            if (confidence > highestConfidence)
            {
                highestConfidence = confidence;
            }
        }

        return highestConfidence;
    }

    /// <summary>
    /// Returns the gap confidence for the given BPM
    /// </summary>
    public double GetConfidenceForBPM(int sampleRate, double bpm, Polynomial polyFit)
    {
        Array.Clear(wrappedOnsets, 0, wrappedOnsets.Length);

        var intervalf = sampleRate * 60.0 / bpm;
        var interval = (int)(intervalf + 0.5);
        for (var i = 0; i < onsetCount; ++i)
        {
            var pos = (int)(onsets[i].Position % intervalf);
            wrappedPos[i] = pos;
            wrappedOnsets[pos] += onsets[i].Strength;
        }

        // Record the amount of support for each gap value.
        var highestConfidence = 0.0;
        for (var i = 0; i < onsetCount; ++i)
        {
            var pos = wrappedPos[i];
            var confidence = GapConfidence(pos, interval);
            var offbeatPos = (pos + (interval / 2)) % interval;
            confidence += GapConfidence(offbeatPos, interval) * 0.5;

            if (confidence > highestConfidence)
            {
                highestConfidence = confidence;
            }
        }

        // Normalize the confidence value.
        highestConfidence -= polyFit.Evaluate(intervalf);

        return highestConfidence;
    }

    /// <summary>
    /// Returns the most promising offset for the given BPM value.
    /// </summary>
    public double GetBaseOffsetValue(int sampleRate, double bpm)
    {
        Array.Clear(wrappedOnsets, 0, wrappedOnsets.Length);

        // Make a histogram of onset strengths for every position in the interval.
        var intervalf = sampleRate * 60.0 / bpm;
        var interval = (int)(intervalf + 0.5);
        for (var i = 0; i < onsetCount; ++i)
        {
            var pos = (int)(onsets[i].Position % intervalf);
            wrappedPos[i] = pos;
            wrappedOnsets[pos] += 1.0;
        }

        // Record the amount of support for each gap value.
        var highestConfidence = 0.0;
        var offsetPos = 0;
        for (var i = 0; i < onsetCount; ++i)
        {
            var pos = wrappedPos[i];
            var confidence = GapConfidence(pos, interval);
            var offbeatPos = (pos + (interval / 2)) % interval;
            confidence += GapConfidence(offbeatPos, interval) * 0.5;

            if (confidence > highestConfidence)
            {
                highestConfidence = confidence;
                offsetPos = pos;
            }
        }

        return (double)offsetPos / sampleRate;
    }

    // Creates weights for a hamming window of length n.
    private double[] CreateHammingWindow(int n)
    {
        var output = new double[n];
        var t = 6.2831853071795864 / (n - 1);
        for (var i = 0; i < n; ++i) output[i] = 0.54 - (0.46 * Math.Cos(i * t));
        return output;
    }
}
