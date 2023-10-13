using AudioSync.OnsetDetection;
using AudioSync.OnsetDetection.Structures;
using AudioSync.Structures;
using AudioSync.Util;
using MathNet.Numerics;

namespace AudioSync;

public sealed class SyncAnalyser
{
    internal const int INTERVAL_DELTA = 1;
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

    public Task<IList<SyncResult>> RunAsync(double[] monoAudioData, int sampleRate, int blockSize = 2048, int hopSize = 256)
        => Task.Run(() => Run(monoAudioData, sampleRate, blockSize, hopSize));

    public IList<SyncResult> Run(double[] monoAudioData, int sampleRate, int blockSize = 2048, int hopSize = 256)
    {
        // From mattmora's testing, Complex Domain with 0.1 threshold seems to give best results
        var onsetDetection = new OnsetDetector(OnsetType.ComplexDomain, blockSize, hopSize, sampleRate)
        {
            Threshold = 0.1f
        };

        Span<double> monoSpan = monoAudioData.AsSpan();
        Span<double> hopData = stackalloc double[hopSize];
        var samples = monoAudioData.Length;
        var onsetOutput = 0.0;
        var detectedOnsets = new List<Onset>();

        // Find offsets in audio
        for (var i = 0; i + hopSize < samples; i += hopSize)
        {
            // We copy blocks of hop data at a time to save iterations 
            monoSpan.Slice(i, hopSize).CopyTo(hopData);

            // Perform onset detection in hopes to find an onset
            // TODO: Move onset output from ref value to method output
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

        // We need at least two onsets to determine BPM, return empty.
        if (detectedOnsets.Count < 2)
        {
            return Array.Empty<SyncResult>();
        }

        // Calculate BPM using our detected onsets
        var syncResults = CalculateBPM(detectedOnsets, sampleRate);

        // Further calculate offset for our sync results
        CalculateOffset(detectedOnsets, syncResults, monoAudioData, sampleRate);

        // bingo.
        return syncResults;
    }

    #region BPM Calculation
    private IList<SyncResult> CalculateBPM(List<Onset> onsets, int sampleRate)
    {
        var results = new List<SyncResult>();

        // Calculate interval range to test
        var minInterval = (int)((sampleRate * 60 / maximumBPM) + 0.5);
        var maxInterval = (int)((sampleRate * 60 / minimumBPM) + 0.5);
        var numIntervals = maxInterval - minInterval;

        var intervalTester = new IntervalTester(sampleRate, minInterval, maxInterval);
        var gapData = new GapData(maxInterval, INTERVAL_DOWNSAMPLE, onsets);

        // Fill some coarse intervals to try and pick out interesting results
        var coarseIntervals = (numIntervals + INTERVAL_DELTA - 1) / INTERVAL_DELTA;
        intervalTester.FillCoarseIntervals(gapData, coarseIntervals, INTERVAL_DELTA);

        var fitness = intervalTester.Fitness;

        // Approximate a fitness curve using a cubic polynomial fit
        var polyX = new double[coarseIntervals];
        var polyY = new double[coarseIntervals];

        for (var i = 0; i < coarseIntervals; i++)
        {
            var idx = i * INTERVAL_DELTA;
            var interval = minInterval + idx;

            polyX[i] = interval;
            polyY[i] = fitness[idx];
        }

        var poly = Polynomial.Fit(polyX, polyY, 3);

        // Normalize fitness values with our new polynomial
        var maxFitness = 0.001;
        for (var i = 0; i < numIntervals; i += INTERVAL_DELTA)
        {
            fitness[i] -= poly.Evaluate(i + minInterval);
            maxFitness = Math.Max(maxFitness, fitness[i]);
        }

        // Refine results around our best intervals
        var fitnessThreshold = maxFitness * 0.4;
        for (var i = 0; i < numIntervals; i += INTERVAL_DELTA)
        {
            if (fitness[i] <= fitnessThreshold) continue;

            var resultBPM = intervalTester.IntervalToBPM(i);
            var resultFitness = fitness[i];

            // Offset to be calculated later
            results.Add(new SyncResult(resultFitness, resultBPM, 0.0));
        }

        // Stop downsampling for more precision
        gapData.Downsample = 0;

        // Sort by fitness
        // REVIEW: Is this first sort necessary? I dont believe the rounding/de-duplication methods strictly require an ordered list.
        results.Sort((a, b) => a.Fitness.CompareTo(b.Fitness));

        // Round values close to whole BPMs and remove lesser duplicates
        RoundBPMValues(gapData, results, poly, sampleRate, 0.1, 1);
        RemoveDuplicates(results, 0.1);

        // More general rounding, gives more generous results. Remove exact duplicates from the rounding process
        RoundBPMValues(gapData, results, poly, sampleRate, 0.3, ROUNDING_THRESHOLD);
        RemoveDuplicates(results, 0.0);

        // Sort by fitness after rounding and de-duplication
        results.Sort((a, b) => a.Fitness.CompareTo(b.Fitness));

        // If we have very close results, we perform one last confidence pass as a second check.
        if (results.Count >= 2 && results[0].Fitness / results[1].Fitness < 1.05)
        {
            // Re-calculate confidence/fitness
            for (var i = 0; i < results.Count; i++)
            {
                results[i] = results[i] with
                {
                    Fitness = gapData.GetConfidenceForBPM(sampleRate, results[i].BPM, poly)
                };
            }

            // Have to re-sort with our re-calculated results.
            results.Sort((a, b) => a.Fitness.CompareTo(b.Fitness));
        }

        // Some implementations pick out the top N results.
        // To prevent allocations on our side, we will just return all of them, and let the consumer pick the top results.
        return results;
    }

    // Rounds BPM values that are close to integer values.
    private void RoundBPMValues(GapData gapData, IList<SyncResult> results, Polynomial polyFit, int sampleRate, double range, double threshold)
    {
        for (var i = 0; i < results.Count; i++)
        {
            var result = results[i];

            var roundedBPM = Math.Round(result.BPM);
            var diff = Math.Abs(roundedBPM - result.BPM);

            if (diff > range) continue;

            var oldConfidence = gapData.GetConfidenceForBPM(sampleRate, result.BPM, polyFit);
            var newConfidence = gapData.GetConfidenceForBPM(sampleRate, roundedBPM, polyFit);

            if (newConfidence < oldConfidence * threshold) continue;

            results[i] = result with
            {
                BPM = roundedBPM,
                Fitness = newConfidence
            };
        }
    }

    // Removes near-duplicates or multiples of existing BPMs
    private void RemoveDuplicates(IList<SyncResult> results, double precision)
    {
        // Sweep forwards
        for (var i = 0; i < results.Count; i++)
        {
            var bpm = results[i].BPM;
            var doubled = bpm * 2;
            var halved = bpm * 0.5;

            // Check backwards so we aren't messing up the forwards sweep
            for (var j = results.Count - 1; j > i; j--)
            {
                var other = results[j].BPM;

                var bpmDifference = Math.Abs(bpm - other);
                var halfBPMDifference = Math.Abs(halved - other);
                var doubledBPMDifference = Math.Abs(doubled - other);

                // Kinda wish Math had params overloads but oh well
                // This just takes the minimum difference between normal/half/doubled BPM and the BPM we are checking
                var minDifference = Math.Min(Math.Min(bpmDifference, halfBPMDifference), doubledBPMDifference);

                if (minDifference > precision) continue;

                // Remove duplicate BPM
                results.RemoveAt(j);
            }
        }
    }
    #endregion

    // Calculates the best offsets for each BPM candidate.
    private void CalculateOffset(List<Onset> onsets, IList<SyncResult> results, double[] monoSamples, int sampleRate)
    {
        // Create gapdata buffers for testing.
        var maxInterval = 0.0;
        foreach (var result in results) maxInterval = Math.Max(maxInterval, sampleRate * 60.0 / result.BPM);
        var gapData = new GapData((int)(maxInterval + 1.0), 1, onsets);

        for (var i = 0; i < results.Count; i++)
        {
            var result = results[i];
            var bpm = result.BPM;
            var beat = result.Beat;

            var baseOffset = gapData.GetBaseOffsetValue(sampleRate, bpm);
            var adjustedOffset = AdjustForOffbeats(monoSamples, sampleRate, baseOffset, bpm);

            if (adjustedOffset > beat / 2) adjustedOffset -= beat;

            results[i] = result with
            {
                Offset = adjustedOffset
            };
        }
    }

    // Compares each offset to its corresponding offbeat value, and selects the most promising one.
    private double AdjustForOffbeats(double[] monoSamples, int sampleRate, double offset, double bpm)
    {
        var sampleCount = monoSamples.Length;

        // Create a slope representation of the waveform.
        Span<double> slopes = stackalloc double[sampleCount];
        ComputeSlopes(monoSamples, ref slopes, sampleCount, sampleRate);

        // Determine the offbeat sample position.
        var secondsPerBeat = 60.0 / bpm;
        var offbeat = offset + (secondsPerBeat * 0.5);
        if (offbeat > secondsPerBeat) offbeat -= secondsPerBeat;

        // Calculate the support for both sample positions.
        var end = (double)sampleCount;
        var interval = secondsPerBeat * sampleRate;
        var posA = offset * sampleRate;
        var posB = offbeat * sampleRate;
        var sumA = 0.0;
        var sumB = 0.0;
        for (; posA < end && posB < end; posA += interval, posB += interval)
        {
            sumA += slopes[(int)posA];
            sumB += slopes[(int)posB];
        }

        // Return the offset with the highest support.
        return (sumA >= sumB) ? offset : offbeat;
    }

    // Compute slopes/derivatives of our samples
    private void ComputeSlopes(double[] samples, ref Span<double> output, int numFrames, int samplerate)
    {
        var wh = samplerate / 20;
        if (numFrames < wh * 2) return;

        // Initial sums of the left/right side of the window.
        var sumL = 0.0;
        var sumR = 0.0;
        for (var i = 0; i < wh; i++)
        {
            sumL += Math.Abs(samples[i]);
            sumR += Math.Abs(samples[i + wh]);
        }

        // Slide window over the samples.
        var scalar = 1.0 / wh;
        for (int i = wh, end = numFrames - wh; i < end; ++i)
        {
            // Determine slope value.
            output[i] = Math.Max(0.0, (sumR - sumL) * scalar);

            // Move window.
            var cur = Math.Abs(samples[i]);
            sumL -= Math.Abs(samples[i - wh]);
            sumL += cur;
            sumR -= cur;
            sumR += Math.Abs(samples[i + wh]);
        }
    }
}
