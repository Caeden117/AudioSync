using System.Numerics;

namespace AudioSync.Util.Tests.FFT;

/*
 * Compare our custom FFT implementation with a known source (DSPLib by Steve Hageman).
 * Given a window size (powers of 2), we calculate various FFTs in DSPLib, then compare the results against the same FFTs calculated with AudioSync.Util.
 */

[TestFixture(8)]
[TestFixture(16)]
[TestFixture(32)]
[TestFixture(64)]
[TestFixture(128)]
[TestFixture(256)]
[TestFixture(512)]
[TestFixture(1024)]
[TestFixture(2048)]
public class FFTTests
{
    // [Repeat] isn't supported in [TestFixtures], so I need to manually repeat my test.
    private const int testIterations = 256;

    private static readonly Random rng = new();

    private readonly int windowSize;
    private readonly int realSize;
    private readonly DSPLib.FFT dspLibFFT;
    private readonly Util.FFT.FFT audioSyncFFT;

    public FFTTests(int windowSize)
    {
        this.windowSize = windowSize;

        // The size of the real half of the FFT, which is returned by both DSPLib and AudioSync.
        realSize = (windowSize / 2) + 1;

        dspLibFFT = new();
        dspLibFFT.Initialize((uint)windowSize);

        audioSyncFFT = new(windowSize);
    }

    [Test]
    public void Test()
    {
        // Manually repeat tests (see comment on "testIterations" constant)
        for (var i = 0; i < testIterations; i++)
        {
            Test_Internal();
        }
    }

    // Fill an array with random numbers, comparing FFT results between our known library and AudioSync.
    private void Test_Internal()
    {
        var doubles = new double[windowSize];
        var doublesSpan = doubles.AsSpan();

        for (var i = 0; i < windowSize; i++)
        {
            doubles[i] = rng.NextDouble();
        }

        var actual = new Complex[realSize];
        var actualSpan = actual.AsSpan();

        var expected = dspLibFFT.Execute(doubles);

        audioSyncFFT.Execute(in doublesSpan, ref actualSpan);

        // We need to manually compare each Real component within basically floating point error
        // (I personally don't mind small differences, but if our FFT starts giving wildly different results, that means something is probably wrong.)
        // As explained previously, both FFT implementations only return the real half,
        // so we only need to compare against the Real components of our Complex results.
        for (var j = 0; j < realSize; j++)
        {
            Assert.That(actual[j].Real, Is.EqualTo(expected[j].Real).Within(0.0000000001));
        }
    }
}
