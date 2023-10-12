namespace AudioSync.Util;

public static partial class Utils
{
    /// <summary>
    /// Converts a multi-channel <see cref="float"/> audio sample array into a mono-channel <see cref="double"/> sample array, ready for sync analysis.
    /// </summary>
    public static double[] ConvertToMonoSamples(this float[] audioData, int channels)
    {
        var numMonoSamples = audioData.Length / channels;
        var monoSamples = new double[numMonoSamples];

        for (var i = 0; i < numMonoSamples; i++)
        {
            for (var c = 0; c < channels; c++)
            {
                monoSamples[i] += audioData[(i * channels) + c];
            }

            monoSamples[i] /= channels;
        }

        return monoSamples;
    }

    /// <summary>
    /// Converts an already mono-channel <see cref="float"/> audio sample array into a mono-channel <see cref="double"/> sample array, ready for sync analysis.
    /// </summary>
    public static double[] ConvertToMonoSamples(this float[] monoAudio) => Array.ConvertAll(monoAudio, f => (double)f);

    /// <summary>
    /// Converts a multi-channel audio sample array into a mono-channel sample array, ready for sync analysis.
    /// </summary>
    public static double[] ConvertToMonoSamples(this double[] audioData, int channels)
    {
        var numMonoSamples = audioData.Length / channels;
        var monoSamples = new double[numMonoSamples];

        for (var i = 0; i < numMonoSamples; i++)
        {
            for (var c = 0; c < channels; c++)
            {
                monoSamples[i] += audioData[(i * channels) + c];
            }

            monoSamples[i] /= channels;
        }

        return monoSamples;
    }
}
