using AudioSync.OnsetDetection.Structures;
using AudioSync.Util.Structures;

namespace AudioSync.OnsetDetection.Util;

internal sealed class SpectralDescription
{
    private readonly OnsetType onsetType;
    private readonly SpectralFunction method;
    private readonly double[]? oldMagnitude;
    private readonly double[]? currentMeasureVector;
    private readonly double[]? lastFrameVector;
    private readonly double[]? secondLastFrameVector;

    public SpectralDescription(OnsetType type, int size)
    {
        var rSize = (size / 2) + 1;
        onsetType = type;

        switch (onsetType)
        {
            case OnsetType.HighFrequencyContent:
                break;
            case OnsetType.ComplexDomain:
                oldMagnitude = new double[rSize];
                currentMeasureVector = new double[rSize];
                lastFrameVector = new double[rSize];
                secondLastFrameVector = new double[rSize];
                break;
            case OnsetType.KullbackLiebler:
            case OnsetType.SpectralFlux:
                oldMagnitude = new double[rSize];
                break;
        }

        method = onsetType switch
        {
            OnsetType.HighFrequencyContent => HighFrequencyContent,
            OnsetType.ComplexDomain => ComplexDomain,
            OnsetType.KullbackLiebler => KullbackLiebler,
            OnsetType.SpectralFlux => SpectralFlux,
            _ => throw new ArgumentException($"{nameof(type)} value ({type}) is not a supported Onset Type.")
        };
    }

    public void Do(in Span<Polar> fftGrain, ref double onset) => method?.Invoke(in fftGrain, ref onset);

    private void HighFrequencyContent(in Span<Polar> fftGrain, ref double onset)
    {
        onset = default;

        for (var i = 0; i < fftGrain.Length; i++)
        {
            onset += (i + 1) * fftGrain[i].Norm;
        }
    }

    private void ComplexDomain(in Span<Polar> fftGrain, ref double onset)
    {
        if (oldMagnitude == null) throw new ArgumentNullException(nameof(oldMagnitude));
        if (currentMeasureVector == null) throw new ArgumentNullException(nameof(currentMeasureVector));
        if (lastFrameVector == null) throw new ArgumentNullException(nameof(lastFrameVector));
        if (secondLastFrameVector == null) throw new ArgumentNullException(nameof(secondLastFrameVector));

        var bins = fftGrain.Length;
        onset = default;

        for (var i = 0; i < bins; i++)
        {
            ref var oldMag = ref oldMagnitude[i];
            ref var grain = ref fftGrain[i];

            // compute predicted phase
            currentMeasureVector[i] = (2.0 * lastFrameVector[i]) - secondLastFrameVector[i];

            // compute euclidean distance in the complex domain
            var euclideanDistance = (oldMag * oldMag) + (grain.Norm * grain.Norm);
            euclideanDistance -= 2 * oldMag * grain.Norm * Math.Cos(currentMeasureVector[i] - grain.Phase);
            euclideanDistance = Math.Abs(euclideanDistance);
            euclideanDistance = Math.Sqrt(euclideanDistance);
            onset += euclideanDistance;

            // Push back frames
            secondLastFrameVector[i] = lastFrameVector[i];
            lastFrameVector[i] = grain.Phase;

            // Push back magnitude
            oldMag = grain.Norm;
        }
    }

    private void KullbackLiebler(in Span<Polar> fftGrain, ref double onset)
    {
        if (oldMagnitude == null) throw new ArgumentNullException(nameof(oldMagnitude));

        onset = default;

        for (var i = 0; i < fftGrain.Length; i++)
        {
            ref var grain = ref fftGrain[i];
            ref var mag = ref oldMagnitude[i];

            onset += grain.Norm * Math.Log(1 + (grain.Norm / (mag + 1e-1)));
            mag = grain.Norm;
        }

        if (double.IsNaN(onset)) onset = default;
    }

    private void SpectralFlux(in Span<Polar> fftGrain, ref double onset)
    {
        if (oldMagnitude == null) throw new ArgumentNullException(nameof(oldMagnitude));

        onset = default;

        for (var i = 0; i < fftGrain.Length; i++)
        {
            ref var grain = ref fftGrain[i];
            ref var mag = ref oldMagnitude[i];

            if (grain.Norm > mag)
            {
                onset += grain.Norm - mag;
            }

            mag = grain.Norm;
        }
    }


    private delegate void SpectralFunction(in Span<Polar> fftGrain, ref double onset);
}
