using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace AudioSync.Util.Tests.Polar;

// Test conversion of Complex to Polar by randomly generating N complex structures, converting them all to Polar, then verifying results.
public class ToPolarTests
{
    private const int N = 100;

    private Complex[] complexes;

    [SetUp]
    public void Setup()
    {
        var rng = new Random();
        complexes = new Complex[N];

        for (int i = 0; i < N; i++)
        {
            complexes[i] = new Complex(
                rng.NextDouble(),
                rng.NextDouble());
        }
    }

    [Test]
    [SuppressMessage("Assertion", "NUnit2045:Use Assert.Multiple",
        Justification = "Spans cannot be accessed in lambda expressions.")]
    public void ConvertComplexToPolar()
    {
        Span<Complex> complexSpans = complexes.AsSpan();
        Span<Structures.Polar> polars = stackalloc Structures.Polar[N];
    
        Utils.ToPolar(in complexSpans, ref polars);
    
        for (var i = 0; i < N; i++)
        {
            Assert.That(polars[i].Norm, Is.EqualTo(complexSpans[i].Magnitude).Within(0.00001));
            Assert.That(polars[i].Phase, Is.EqualTo(complexSpans[i].Phase).Within(0.00001));
        }
    }
}
