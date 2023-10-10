using AudioSync.Util;

namespace AudioSync.Tests.Push;

public class PushTestSimple
{
    [Test]
    public void Push()
    {
        Span<double> numbers = stackalloc double[] { 0, 1, 2 };

        Utils.Push(ref numbers, 3);

        Assert.That(numbers[0], Is.EqualTo(1.0));
        Assert.That(numbers[1], Is.EqualTo(2.0));
        Assert.That(numbers[2], Is.EqualTo(3.0));
    }
}