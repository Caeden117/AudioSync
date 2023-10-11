using AudioSync.Util;

namespace AudioSync.Tests.Median;

public class SimpleMedianTests
{
    [Test]
    public void SimpleMedian()
    {
        Span<double> span = stackalloc double[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

        var result = Utils.Median(ref span);

        Assert.That(result, Is.EqualTo(3.0));
    }

    [Test]
    public void SimpleMedianCompareAgainstAubio()
    { 
        Span<double> span = stackalloc double[] { 1.0, 2.0, 3.0, 4.0, 5.0 };
        double[] array = new double[] { 1.0, 2.0, 3.0, 4.0, 5.0 };

        var ourResult = Utils.Median(ref span);
        var aubioResult = AubioCompareMethods.Median(array);

        Assert.That(ourResult, Is.EqualTo(aubioResult));
    }
}
