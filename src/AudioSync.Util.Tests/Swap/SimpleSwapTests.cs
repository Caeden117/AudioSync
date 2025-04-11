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
            Span<float> span = stackalloc float[] { 1.0f };

            Utils.Swap(ref span);
        });
    }

    [Test]
    public void ThrowsOnEmpty()
    {
        Assert.Throws<AudioSyncFatalException>(() =>
        {
            Span<float> span = stackalloc float[0];

            Utils.Swap(ref span);
        });
    }
}
