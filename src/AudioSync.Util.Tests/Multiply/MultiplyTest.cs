using AudioSync.Util.Exceptions;

namespace AudioSync.Util.Tests.Multiply;

public class MultiplyTest
{
    [Test]
    public void SimpleMultiplyTest()
    {
        Span<double> left = stackalloc double[] { 1, 2, 3 }; 
        Span<double> right = stackalloc double[] { 2, 2, 2 };
        Span<double> result = stackalloc double[3];

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
            Span<double> left = stackalloc double[10];
            Span<double> right = stackalloc double[5];
            Span<double> result = stackalloc double[10];

            // Should throw because the length of our left and right operands are not equal.
            Utils.Multiply(in left, in right, ref result);
        });
    }

    [Test]
    public void ThrowsOnResultLengthMismatch()
    {
        Assert.Throws<AudioSyncFatalException>(() =>
        {
            Span<double> left = stackalloc double[10];
            Span<double> right = stackalloc double[10];
            Span<double> result = stackalloc double[5];

            // Should throw because the length of our result operand isnt equal to the operands
            Utils.Multiply(in left, in right, ref result);
        });
    }

    [Test]
    public void SIMDThrowsOnOperandsLengthMismatch()
    {
        Assert.Throws<AudioSyncFatalException>(() =>
        {
            Span<double> left = stackalloc double[10];
            Span<double> right = stackalloc double[5];
            Span<double> result = stackalloc double[10];

            // Should throw because the length of our left and right operands are not equal.
            Utils.MultiplySIMD(in left, in right, ref result);
        });
    }

    [Test]
    public void SIMDThrowsOnResultLengthMismatch()
    {
        Assert.Throws<AudioSyncFatalException>(() =>
        {
            Span<double> left = stackalloc double[10];
            Span<double> right = stackalloc double[10];
            Span<double> result = stackalloc double[5];

            // Should throw because the length of our result operand isnt equal to the operands
            Utils.MultiplySIMD(in left, in right, ref result);
        });
    }
}
