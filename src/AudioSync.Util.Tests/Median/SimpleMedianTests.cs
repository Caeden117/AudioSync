using AudioSync.Util.Exceptions;
namespace AudioSync.Util.Tests.Median;

public class SimpleMedianTests
{
    [Test]
    public void SimpleMedian()
    {
        Span<float> span = stackalloc float[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f };

        var result = Utils.Median(ref span);

        Assert.That(result, Is.EqualTo(3.0f));
    }

    [Test]
    public void SimpleMedianCompareAgainstAubio()
    { 
        Span<float> span = stackalloc float[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f };
        double[] array = new double[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

        var ourResult = Utils.Median(ref span);
        var aubioResult = AubioCompareMethods.Median(array);

        Assert.That(ourResult, Is.EqualTo(aubioResult));
    }

    [Test]
    public void ThrowsOnEmpty()
    {
        Assert.Throws<AudioSyncFatalException>(() =>
        {
            var arr = Array.Empty<float>();
            var span = arr.AsSpan();

            var result = Utils.Median(ref span);
        });
    }
}
