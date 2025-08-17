using AudioSync.OnsetDetection;
using AudioSync.OnsetDetection.Structures;
using AudioSync.Structures;
using AudioSync.Util;
using MathNet.Numerics;

namespace AudioSync;

public sealed class SyncAnalyser
{
    private const int INTERVAL_DELTA = 1;
    private const int INTERVAL_DOWNSAMPLE = 5;

    // Number of threads to use for parallel onset detection.
    // More is better, but diminishing returns after a certain point.
    // (Too many threads can also reduce onset accuracy)
    private const int NUM_THREADS = 8;

    // Max number of results to return.
    private const int MAX_RESULTS = 10;

    // Size of window around onset sample used to calculate onset strength, can significantly affect results
    // 200 is from the original code, not sure where it comes from, but it seems to work well
    private const int STRENGTH_WINDOW_SIZE = 200;

    // Fitness ratio of rounded to un-rounded required to accept a rounded BPM
    private const float ROUNDING_THRESHOLD = 0.95f;

    private readonly float minimumBPM;
    private readonly float maximumBPM;

    public SyncAnalyser(float minimumBPM = 85, float maximumBPM = 205)
    {
        this.minimumBPM = minimumBPM;
        this.maximumBPM = maximumBPM;
    }

    public Task<IList<SyncResult>> RunAsync(float[] monoAudioData, int sampleRate, int blockSize = 2048, int hopSize = 256)
        => Task.Run(() => Run(monoAudioData, sampleRate, blockSize, hopSize));

    public IList<SyncResult> Run(float[] monoAudioData, int sampleRate, int blockSize = 2048, int hopSize = 256)
    {
        // From mattmora's testing, Complex Domain with 0.1 threshold seems to give best results
        var onsetDetection = new OnsetDetector(OnsetType.ComplexDomain, blockSize, hopSize, sampleRate)
        {
            Threshold = 0.1f
        };

        Span<float> monoSpan = monoAudioData.AsSpan();
        Span<float> hopData = stackalloc float[hopSize];
        var samples = monoAudioData.Length;
        var detectedOnsets = new List<Onset>();

        // Perform onset detection in chunks across multiple threads
        // Technically it *does* lose accuracy on thread boundaries but the speed increase is worth it
        Parallel.For(0, NUM_THREADS, thread =>
        {
            var samplesPerThread = samples / NUM_THREADS;
            var startThread = thread * samplesPerThread;
            var endThread = (thread + 1) * samplesPerThread;

            FindOnsetThreaded(startThread, endThread, detectedOnsets, monoAudioData, blockSize, hopSize, sampleRate);
        });

        // We need at least two onsets to determine BPM, return empty.
        if (detectedOnsets.Count < 2)
        {
            return Array.Empty<SyncResult>();
        }

        // Calculate BPM using our detected onsets
        var syncResults = CalculateBPM(detectedOnsets, sampleRate);

        // Further calculate offset for our sync results
        CalculateOffset(detectedOnsets, syncResults, monoAudioData, sampleRate);

        // Limit results to max results allowed
        if (syncResults.Count > MAX_RESULTS)
            syncResults.RemoveRange(MAX_RESULTS, syncResults.Count - MAX_RESULTS);

        // bingo.
        return syncResults;
    }

    private void FindOnsetThreaded(int frameStart, int frameEnd, List<Onset> detectedOnsets, float[] monoAudioData, int blockSize, int hopSize, int sampleRate)
    {
        // From mattmora's testing, Complex Domain with 0.1 threshold seems to give best results
        var onsetDetection = new OnsetDetector(OnsetType.ComplexDomain, blockSize, hopSize, sampleRate)
        {
            Threshold = 0.1f
        };

        Span<float> monoSpan = monoAudioData.AsSpan();
        Span<float> hopData = stackalloc float[hopSize];
        var onsetOutput = 0.0f;

        // Find onsets
        for (var i = frameStart; i + hopSize < frameEnd; i += hopSize)
        {
            // We copy blocks at a time to save iterations 
            monoSpan.Slice(i, hopSize).CopyTo(hopData);

            // Perform onset detection in hopes to find an onset
            // TODO: Move onset output from ref value to method output
            onsetDetection.DetectOnsets(in hopData, ref onsetOutput);

            // If we do not find an onset, do not bother
            if (onsetOutput < 1) continue;

            // Sample position of our detected onset
            var position = onsetDetection.LastOffset;

            // Calculate strength of newly detected onset by taking the average of samples around the onset position
            var windowMin = Math.Max(0, position - (STRENGTH_WINDOW_SIZE / 2));
            var windowMax = Math.Min(monoAudioData.Length, position + (STRENGTH_WINDOW_SIZE / 2));

            var strength = 0.0f;
            Parallel.For(windowMin, windowMax, j =>
            {
                Interlocked.Exchange(ref strength, strength + MathF.Abs(monoAudioData[j]));
            });
            strength /= MathF.Max(1, windowMax - windowMin);

            detectedOnsets.Add(new(onsetDetection.LastOffset, strength));
        }
    }

    #region BPM Calculation
    private List<SyncResult> CalculateBPM(List<Onset> onsets, int sampleRate)
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
        // How I wish I have these be stack allocated, but alas I would need to re-write the Polynomial portion of MathNET.Numerics
        //   to support Span<T> methods.
        // REVIEW: Move these to ArrayPool instead?
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
        var maxFitness = 0.001f;
        for (var i = 0; i < numIntervals; i += INTERVAL_DELTA)
        {
            fitness[i] -= (float)poly.Evaluate(i + minInterval);
            maxFitness = MathF.Max(maxFitness, fitness[i]);
        }

        // Create initial results around our best intervals
        var fitnessThreshold = maxFitness * 0.4;
        for (var i = 0; i < numIntervals; i += INTERVAL_DELTA)
        {
            if (fitness[i] <= fitnessThreshold) continue;

            var resultBPM = intervalTester.IntervalToBPM(i);
            var resultFitness = fitness[i];

            // Offset to be calculated later
            results.Add(new SyncResult(resultFitness, resultBPM, 0.0f));
        }

        // Stop downsampling for more precision
        gapData.Downsample = 0;

        // Sort by fitness
        // REVIEW: Is this first sort necessary? I dont believe the rounding/de-duplication methods strictly require an ordered list.
        results.Sort((a, b) => b.Fitness.CompareTo(a.Fitness));

        // Round values close to whole BPMs and remove lesser duplicates
        RoundBPMValues(gapData, results, poly, sampleRate, 0.1f, 1);
        RemoveDuplicates(results, 0.1f);

        // More general rounding, gives more generous results, followed by another de-duplication.
        RoundBPMValues(gapData, results, poly, sampleRate, 0.3f, ROUNDING_THRESHOLD);
        RemoveDuplicates(results, 0.0f);

        // Sort by fitness after rounding and de-duplication
        results.Sort((a, b) => b.Fitness.CompareTo(a.Fitness));

        // If we have very close results, we perform one last confidence pass as a second check.
        // REVIEW: On average, it may be faster to always re-calculate fitness and sort.
        //   Thereby always needing only one sort, but at the cost of an iteration to re-calculate fitness.
        //   is this trade-off worth it?
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
            results.Sort((a, b) => b.Fitness.CompareTo(a.Fitness));
        }

        // Some implementations pick out the top N results.
        // To prevent allocations on our side, we will just return all of them, and let the consumer pick the top results.
        return results;
    }

    // Rounds BPM values that are close to integer values.
    private void RoundBPMValues(GapData gapData, IList<SyncResult> results, Polynomial polyFit, int sampleRate, float range, float threshold)
    {
        for (var i = 0; i < results.Count; i++)
        {
            var result = results[i];

            // Calculate the difference between our BPM and a theoretical rounded BPM
            var roundedBPM = MathF.Round(result.BPM);
            var diff = MathF.Abs(roundedBPM - result.BPM);

            // Ignore this result if the difference proves too much to bother
            if (diff > range) continue;

            // Calculate and compare the differences between our current BPM and our rounded BPM
            var oldConfidence = gapData.GetConfidenceForBPM(sampleRate, result.BPM, polyFit);
            var newConfidence = gapData.GetConfidenceForBPM(sampleRate, roundedBPM, polyFit);

            // Ignore this result if the difference proves too much to bother
            if (newConfidence < oldConfidence * threshold) continue;

            // If our rounded BPM gives better confidence, update the current result.
            results[i] = result with
            {
                BPM = roundedBPM,
                Fitness = newConfidence
            };
        }
    }

    // Removes near-duplicates or multiples of existing BPMs
    private void RemoveDuplicates(IList<SyncResult> results, float precision)
    {
        // Sweep forwards
        for (var i = 0; i < results.Count; i++)
        {
            var bpm = results[i].BPM;
            var doubled = bpm * 2;
            var halved = bpm * 0.5f;

            // Check backwards so we aren't messing up the forwards sweep
            for (var j = results.Count - 1; j > i; j--)
            {
                var other = results[j].BPM;

                var bpmDifference = MathF.Abs(bpm - other);
                var halfBPMDifference = MathF.Abs(halved - other);
                var doubledBPMDifference = MathF.Abs(doubled - other);

                // Kinda wish Math had params overloads but oh well
                // This just takes the minimum difference between normal/half/doubled BPM and the BPM we are checking
                var minDifference = MathF.Min(MathF.Min(bpmDifference, halfBPMDifference), doubledBPMDifference);

                // If difference is too much, this other BPM is likely not a duplicate
                if (minDifference > precision) continue;

                // Remove duplicate BPM
                results.RemoveAt(j);
            }
        }
    }
    #endregion

    // Calculates the best offsets for each BPM candidate.
    private void CalculateOffset(List<Onset> onsets, IList<SyncResult> results, float[] monoSamples, int sampleRate)
    {
        // Create gapdata buffers for testing.
        var maxInterval = 0.0f;
        foreach (var result in results) maxInterval = MathF.Max(maxInterval, sampleRate * 60.0f / result.BPM);
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
    private float AdjustForOffbeats(float[] monoSamples, int sampleRate, float offset, float bpm)
    {
        var sampleCount = monoSamples.Length;

        // Calculate the slopes of all our mono samples.
        // REVIEW: Stackallocing with the entire length of mono samples will almost assuredly overflow the stack.
        //   However, I don't want to allocate garbage with every invocation.
        //   Should I switch to ArrayPool?
        var slopesArr = new float[sampleCount];
        var slopes = slopesArr.AsSpan();
        //Span<double> slopes = stackalloc double[sampleCount];
        ComputeSlopes(monoSamples, ref slopes, sampleCount, sampleRate);

        // Determine the offbeat sample position.
        var secondsPerBeat = 60.0f / bpm;
        var offbeat = offset + (secondsPerBeat * 0.5f);
        if (offbeat > secondsPerBeat) offbeat -= secondsPerBeat;

        // Calculate the support for both sample positions.
        var end = (double)sampleCount;
        var interval = secondsPerBeat * sampleRate;
        var posA = offset * sampleRate;
        var posB = offbeat * sampleRate;
        var sumA = 0.0f;
        var sumB = 0.0f;

        while (posA < sampleCount && posB < sampleCount)
        {
            sumA += slopes[(int)posA];
            sumB += slopes[(int)posB];

            posA += interval;
            posB += interval;
        }

        // Return the offset with the highest support.
        return (sumA >= sumB) ? offset : offbeat;
    }

    // Compute slopes/derivatives of our samples
    private void ComputeSlopes(float[] samples, ref Span<float> output, int numFrames, int samplerate)
    {
        var wh = samplerate / 20;
        if (numFrames < wh * 2) return;

        // Initial sums of the left/right side of the window.
        var sumL = 0.0f;
        var sumR = 0.0f;
        for (var i = 0; i < wh; i++)
        {
            sumL += MathF.Abs(samples[i]);
            sumR += MathF.Abs(samples[i + wh]);
        }

        // Slide window over the samples.
        var scalar = 1.0f / wh;
        for (int i = wh; i < numFrames - wh; i++)
        {
            // Determine slope value.
            output[i] = MathF.Max(0.0f, (sumR - sumL) * scalar);

            // Slide window over.
            var cur = MathF.Abs(samples[i]);
            
            sumL -= MathF.Abs(samples[i - wh]);
            sumL += cur;

            sumR -= cur;
            sumR += MathF.Abs(samples[i + wh]);
        }
    }
}
