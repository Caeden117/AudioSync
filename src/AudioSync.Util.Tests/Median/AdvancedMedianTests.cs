using AudioSync.Util;

namespace AudioSync.Util.Tests.Median;

/*
 * These are various median examples found online.
 */

[TestFixture(new float[] { 1, 3, 3, 6, 7, 8, 9 }, 6)]
[TestFixture(new float[] { 1, 2, 3, 4, 5, 6, 8, 9 }, 4.5f)]
[TestFixture(new float[] { 8, 14, 8, 45, 1, 31, 16, 40, 12, 30, 42, 30, 24 }, 24)]
[TestFixture(new float[] { 4, 3, 7, 8, 4, 5, 12, 4, 5, 3, 2, 3 }, 4)]
[TestFixture(new float[] { 1, 3, 4, 8, 12, 13, 15, 17, 19, 20 }, 12.5f)]
[TestFixture(new float[] { 31, 28, 19, 14, 11, 30, 27, 20 }, 23.5f)]
[TestFixture(new float[] { 4, 5, 8, 12, 15, 17, 18 }, 12)]
[TestFixture(new float[] { 23, 26, 26, 26, 29, 36, 39, 40, 42, 42, 48, 49 }, 37.5f)]
public class AdvancedMedianTests
{
    private readonly float[] numbers;
    private readonly float expected;

    public AdvancedMedianTests(float[] numbers, float expected)
    {
        this.numbers = numbers;
        this.expected = expected;
    }

    [Test]
    public void Median()
    {
        var span = numbers.AsSpan();

        var calculated = Utils.Median(ref span);
    
        Assert.That(calculated, Is.EqualTo(expected));
    }

    [Test]
    public void SafeMedian()
    {
        var span = numbers.AsSpan();

        var calculated = Utils.SafeMedian(in span);

        Assert.That(calculated, Is.EqualTo(expected));
    }
}