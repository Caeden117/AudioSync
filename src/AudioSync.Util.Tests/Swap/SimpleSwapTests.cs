using AudioSync.Util.Exceptions;

namespace AudioSync.Util.Tests.Swap;

// Simple tests of Swap that do not need multiple executions (exception/unhappy paths to keep track of)
internal class SimpleSwapTests
{
    [Test]
    public void ThrowsOnOneElement()
    {
        Assert.Throws<AudioSyncFatalException>(() =>
        {
            Span<double> span = stackalloc double[] { 1.0 };

            Utils.Swap(ref span);
        });
    }

    [Test]
    public void ThrowsOnEmpty()
    {
        Assert.Throws<AudioSyncFatalException>(() =>
        {
            Span<double> span = stackalloc double[0];

            Utils.Swap(ref span);
        });
    }
}
