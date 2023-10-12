using AudioSync.OnsetDetection.Structures;
using AudioSync.OnsetDetection.Util;
using AudioSync.Util;
using AudioSync.Util.Structures;
using System.Diagnostics;

namespace AudioSync.OnsetDetection;

public sealed class OnsetDetector
{
    public int MinInterOnsetInterval { get; set; }

    public double MinInterOnsetIntervalSeconds
    {
        get => MinInterOnsetInterval / (double)sampleRate;
        set => MinInterOnsetInterval = (int)Math.Round(value * sampleRate);
    }

    public double MinInterOnsetIntervalMilliseconds
    {
        get => MinInterOnsetIntervalSeconds * 1000;
        set => MinInterOnsetIntervalSeconds = value / 1000;
    }

    public int LastOffset => lastOnset - Delay;

    public double LastSeconds => LastOffset / (double)sampleRate;

    public double LastMilliseconds => LastSeconds * 1000;

    public double SilenceThreshold { get; set; }

    public int Delay { get; set; }

    public double Threshold
    {
        get => peakPicker.Threshold;
        set => peakPicker.Threshold = value;
    }

    public double Descriptor => spectralOutput[0];

    public double ThresholdedDescriptor => peakPicker.Thresholded;

    public double Compression
    {
        get => applyCompression ? lambdaCompression : 0.0;
        set
        {
            Debug.Assert(value >= 0, "Compression must be greater than or equal to 0.");
            lambdaCompression = value;
            applyCompression = lambdaCompression > 0.0;
        }
    }

    public bool ApplyWhitening { get; set; }

    private readonly PhaseVocoder phaseVocoder;
    private readonly SpectralDescription spectralDescription;
    private readonly PeakPicker peakPicker;
    private readonly AdaptiveWhitening adaptiveWhitening;

    // TODO: Convert to stackalloc in Do method
    private readonly Polar[] phaseVocoderOutput;
    private readonly double[] spectralOutput;

    private readonly int sampleRate;
    private readonly int hopSize;

    private int totalFrames;
    private int lastOnset;
    private bool applyCompression;
    private double lambdaCompression;

    public OnsetDetector(OnsetType onsetType, int bufferSize, int hopSize, int sampleRate)
    {
        this.sampleRate = sampleRate;
        this.hopSize = hopSize;

        phaseVocoder = new(bufferSize, hopSize);
        peakPicker = new();
        spectralDescription = new(onsetType, bufferSize);
        adaptiveWhitening = new(bufferSize, hopSize, sampleRate);

        phaseVocoderOutput = new Polar[bufferSize];
        spectralOutput = new double[1];

        // Default values (before we re-initialize based on onset type)
        Threshold = 0.3;
        Delay = (int)(4.3 * hopSize);
        MinInterOnsetIntervalMilliseconds = 50;
        SilenceThreshold = -70;
        ApplyWhitening = false;
        Compression = 0.0;

        // Change settings based on onset type
        switch (onsetType)
        {
            case OnsetType.HighFrequencyContent:
                Threshold = 0.058;
                Compression = 1;
                break;
            case OnsetType.ComplexDomain:
                Delay = (int)(4.6 * hopSize);
                Threshold = 0.15;
                ApplyWhitening = true;
                Compression = 1;
                break;
            case OnsetType.KullbackLiebler:
                Threshold = 0.35;
                ApplyWhitening = true;
                Compression = 0.02;
                break;
            case OnsetType.SpectralFlux:
                Threshold = 0.18;
                ApplyWhitening = true;
                adaptiveWhitening.RelaxTime = 100;
                adaptiveWhitening.Floor = 1;
                Compression = 10;
                break;
        }

        Reset();
    }

    public void Do(in Span<double> input, ref Span<double> onsets)
    {
        // Execute phase vocoding on our input
        Span<Polar> fftGrain = phaseVocoderOutput.AsSpan();
        phaseVocoder.Do(in input, ref fftGrain);

        // Apply whitening if enabled
        if (ApplyWhitening)
        {
            adaptiveWhitening.Do(ref fftGrain);
        }

        // Apply compression if enabled
        if (applyCompression)
        {
            for (var i = 0; i < fftGrain.Length; i++)
            {
                fftGrain[i] = fftGrain[i].LogMag(lambdaCompression);
            }
        }

        // Execute Spectral Description
        // TODO: WE ONLY USE ONE ELEMENT!! Convert SpectralDescription to directly return the value we want
        var spectralSpan = spectralOutput.AsSpan();
        spectralDescription.Do(in fftGrain, ref spectralSpan);

        // Execute Peak Picker
        peakPicker.Do(in spectralSpan, ref onsets);
        var isOnset = onsets[0];

        if (isOnset > 0)
        {
            // Silent onsets should not count as an onset
            if (Utils.IsSilence(in input, SilenceThreshold))
            {
                isOnset = 0;
            }
            // this is a detected onset
            else
            {
                var newOnset = totalFrames + (int)Math.Round(isOnset * hopSize);

                // Check if this onset far enough away
                if (lastOnset + MinInterOnsetInterval < newOnset)
                {
                    // ...and also that it's not blocked by delay
                    if (lastOnset > 0 && Delay > newOnset)
                    {
                        isOnset = 0.0;
                    }
                    else
                    {
                        lastOnset = Math.Max(Delay, newOnset);
                    }
                }
                else
                {
                    isOnset = 0.0;
                }
            }
        }
        // Check if we are at the beginning of the file with no silence
        else if (totalFrames <= Delay && !Utils.IsSilence(in input, SilenceThreshold))
        {
            var newOnset = totalFrames;
            if (totalFrames == 0 || lastOnset + MinInterOnsetInterval < newOnset)
            {
                isOnset = Delay / hopSize;
                lastOnset = totalFrames + Delay;
            }
        }

        onsets[0] = isOnset;
        totalFrames += hopSize;
        return;
    }

    public void Reset()
    {
        lastOnset = 0;
        totalFrames = 0;
    }
}
