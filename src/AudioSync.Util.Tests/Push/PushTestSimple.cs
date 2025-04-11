using AudioSync.Util.Exceptions;
using System.Diagnostics.CodeAnalysis;

namespace AudioSync.Util.Tests.Push;

public class PushTestSimple
{
    [Test]
    [SuppressMessage("Assertion", "NUnit2045:Use Assert.Multiple", Justification = "Span cannot be accessed in a lambda")]
    public void Push()
    {
        Span<float> numbers = stackalloc float[] { 0, 1, 2 };

        Utils.Push(ref numbers, 3);

        Assert.That(numbers[0], Is.EqualTo(1.0));
        Assert.That(numbers[1], Is.EqualTo(2.0));
        Assert.That(numbers[2], Is.EqualTo(3.0));
    }

    [Test]
    public void PushSingle()
    {
        var arr = new float[1] { 0 };

        var span = arr.AsSpan();

        Utils.Push(ref span, 1);

        Assert.That(arr[0], Is.EqualTo(1));
    }

    [Test]
    public void ThrowsOnEmpty()
    {
        Assert.Throws<AudioSyncFatalException>(() =>
        {
            var arr = Array.Empty<float>();
            var span = arr.AsSpan();

            Utils.Push(ref span, 2.0f);
        });
    }
}