using AudioSync.Util.Exceptions;

namespace AudioSync.Util.Tests.Multiply;

public class MultiplyTest
{
    [Test]
    public void SimpleMultiplyTest()
    {
        Span<float> left = stackalloc float[] { 1, 2, 3 }; 
        Span<float> right = stackalloc float[] { 2, 2, 2 };
        Span<float> result = stackalloc float[3];

        Utils.Multiply(in left, in right, ref result);

        for (var i = 0; i < result.Length; i++)
        {
            Assert.That(result[i], Is.EqualTo(left[i] * right[i]));
        }
    }

    [Test]
    public void ThrowsOnOperandsLengthMismatch()
    {
        Assert.Throws<AudioSyncFatalException>(() =>
        {
            Span<float> left = stackalloc float[10];
            Span<float> right = stackalloc float[5];
            Span<float> result = stackalloc float[10];

            // Should throw because the length of our left and right operands are not equal.
            Utils.Multiply(in left, in right, ref result);
        });
    }

    [Test]
    public void ThrowsOnResultLengthMismatch()
    {
        Assert.Throws<AudioSyncFatalException>(() =>
        {
            Span<float> left = stackalloc float[10];
            Span<float> right = stackalloc float[10];
            Span<float> result = stackalloc float[5];

            // Should throw because the length of our result operand isnt equal to the operands
            Utils.Multiply(in left, in right, ref result);
        });
    }

    [Test]
    public void SIMDThrowsOnOperandsLengthMismatch()
    {
        Assert.Throws<AudioSyncFatalException>(() =>
        {
            Span<float> left = stackalloc float[10];
            Span<float> right = stackalloc float[5];
            Span<float> result = stackalloc float[10];

            // Should throw because the length of our left and right operands are not equal.
            Utils.MultiplySIMD(in left, in right, ref result);
        });
    }

    [Test]
    public void SIMDThrowsOnResultLengthMismatch()
    {
        Assert.Throws<AudioSyncFatalException>(() =>
        {
            Span<float> left = stackalloc float[10];
            Span<float> right = stackalloc float[10];
            Span<float> result = stackalloc float[5];

            // Should throw because the length of our result operand isnt equal to the operands
            Utils.MultiplySIMD(in left, in right, ref result);
        });
    }
}
