using AudioSync.Structures;
using MathNet.Numerics;

namespace AudioSync.Util;

// Gap confidence evaluation
internal sealed class GapData
{
    public int Downsample
    {
        get => downsample;
        set
        {
            downsample = value;
            hammingWindow = CreateHammingWindow(2048 >> downsample);
        }
    }

    private readonly List<Onset> onsets;
    private readonly int[] wrappedPos;
    private readonly float[] wrappedOnsets;
    private readonly int onsetCount;

    private int downsample;
    private float[] hammingWindow;

    public GapData(int maxInterval, int downsample, List<Onset> onsets)
    {
        this.onsets = onsets;
        onsetCount = onsets.Count;

        // Technically should be removed by setting Downsample directly
        // however C# gives a warning because hammingWindow isn't directly assigned.
        this.downsample = downsample;
        hammingWindow = CreateHammingWindow(2048 >> downsample);

        wrappedPos = new int[onsetCount];
        wrappedOnsets = new float[maxInterval];
    }

    /// <summary>
    /// Returns a confidence value representing the vicinity of nearby onsets
    /// </summary>
    public float GapConfidence(int gapPos, int interval)
    {
        var windowSize = hammingWindow.Length;
        var halfWindowSize = windowSize / 2;
        var area = 0.0f;

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
    public float GetConfidenceForInterval(int interval)
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
        var highestConfidence = 0.0f;
        for (var i = 0; i < onsetCount; ++i)
        {
            var pos = wrappedPos[i];
            var confidence = GapConfidence(pos, reducedInterval);
            var offbeatPos = (pos + (reducedInterval / 2)) % reducedInterval;
            confidence += GapConfidence(offbeatPos, reducedInterval) * 0.5f;

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
    public float GetConfidenceForBPM(int sampleRate, float bpm, Polynomial polyFit)
    {
        Array.Clear(wrappedOnsets, 0, wrappedOnsets.Length);

        var intervalf = sampleRate * 60.0f / bpm;
        var interval = (int)(intervalf + 0.5f);
        for (var i = 0; i < onsetCount; ++i)
        {
            var pos = (int)(onsets[i].Position % intervalf);
            wrappedPos[i] = pos;
            wrappedOnsets[pos] += onsets[i].Strength;
        }

        // Record the amount of support for each gap value.
        var highestConfidence = 0.0f;
        for (var i = 0; i < onsetCount; ++i)
        {
            var pos = wrappedPos[i];
            var confidence = GapConfidence(pos, interval);
            var offbeatPos = (pos + (interval / 2)) % interval;
            confidence += GapConfidence(offbeatPos, interval) * 0.5f;

            if (confidence > highestConfidence)
            {
                highestConfidence = confidence;
            }
        }

        // Normalize the confidence value.
        highestConfidence -= (float)polyFit.Evaluate(intervalf);

        return highestConfidence;
    }

    /// <summary>
    /// Returns the most promising offset for the given BPM value.
    /// </summary>
    public float GetBaseOffsetValue(int sampleRate, double bpm)
    {
        Array.Clear(wrappedOnsets, 0, wrappedOnsets.Length);

        // Make a histogram of onset strengths for every position in the interval.
        var intervalf = sampleRate * 60.0 / bpm;
        var interval = (int)(intervalf + 0.5);
        for (var i = 0; i < onsetCount; ++i)
        {
            var pos = (int)(onsets[i].Position % intervalf);
            wrappedPos[i] = pos;
            wrappedOnsets[pos] += 1.0f;
        }

        // Record the amount of support for each gap value.
        var highestConfidence = 0.0f;
        var offsetPos = 0.0f;
        for (var i = 0; i < onsetCount; ++i)
        {
            var pos = wrappedPos[i];
            var confidence = GapConfidence(pos, interval);
            var offbeatPos = (pos + (interval / 2)) % interval;
            confidence += GapConfidence(offbeatPos, interval) * 0.5f;

            if (confidence > highestConfidence)
            {
                highestConfidence = confidence;
                offsetPos = pos;
            }
        }

        return offsetPos / sampleRate;
    }

    // Creates weights for a hamming window of length n.
    private float[] CreateHammingWindow(int n)
    {
        var output = new float[n];
        var t = 6.2831853071795864f / (n - 1);
        for (var i = 0; i < n; ++i) output[i] = 0.54f - (0.46f * MathF.Cos(i * t));
        return output;
    }
}
