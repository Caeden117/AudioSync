namespace AudioSync.Util.Tests.Peak;

public class SimplePeakPickTests
{
    [Test]
    public void CanFindPeak()
    {
        var peaks = new double[] { 0.0, 1.0, 0.0 };
        var span = peaks.AsSpan();

        var isPeak = Utils.PeakPick(in span, 1);

        Assert.That(isPeak, Is.True);
    }

    [Test]
    public void PeakFailsOnLeft()
    {
        var peaks = new double[] { 1.0, 0.5, 0.0 };
        var span = peaks.AsSpan();

        // This should return false because element 0 is higher.
        var isPeak = Utils.PeakPick(in span, 1);

        Assert.That(isPeak, Is.False);
    }

    [Test]
    public void PeakFailsOnRight()
    {
        var peaks = new double[] { 0.0, 0.5, 1.0 };
        var span = peaks.AsSpan();

        // This should return false because element 2 is higher.
        var isPeak = Utils.PeakPick(in span, 1);

        Assert.That(isPeak, Is.False);
    }

    [Test]
    public void PeakFailsOnNegatives()
    {
        var peaks = new double[] { -1.0, -0.5, -1.0 };
        var span = peaks.AsSpan();

        // This should return false because the "peak" needs to be above 0.0 amplitude.
        var isPeak = Utils.PeakPick(in span, 1);

        Assert.That(isPeak, Is.False);
    }

    [Test]
    public void ThrowsOnLeftmostIndex()
    {
        var peaks = new double[] { 1.0, 1.0, 1.0 };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var span = peaks.AsSpan();

            Utils.PeakPick(in span, 0);
        });
    }

    [Test]
    public void ThrowsOnRightmostIndex()
    {
        var peaks = new double[] { 1.0, 1.0, 1.0 };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            var span = peaks.AsSpan();

            Utils.PeakPick(in span, span.Length - 1);
        });
    }
}
