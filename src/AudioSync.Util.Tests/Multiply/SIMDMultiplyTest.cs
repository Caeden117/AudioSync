namespace AudioSync.Util.Tests.Multiply;

// Not powers of two, forcing a traditional Multiply call
[TestFixture(150)]
[TestFixture(253)]
[TestFixture(1023)]
[TestFixture(972)]
[TestFixture(861)]
[TestFixture(4053)]
[TestFixture(3491)]

// Perfect powers of two
[TestFixture(256)]
[TestFixture(512)]
[TestFixture(1024)]
[TestFixture(2048)]
public class SIMDMultiplyTest
{
    private readonly float[] numbers;
    private readonly float[] multiplyBy;
    private readonly float[] results;

    public SIMDMultiplyTest(int n)
    {
        numbers = new float[n];
        multiplyBy = new float[n];
        results = new float[n];

        var rng = new Random();

        for (int i = 0; i < n; i++)
        {
            numbers[i] = rng.Next(0, 1024);
            multiplyBy[i] = rng.Next(0, 100);
        }
    }

    [Test]
    public void SIMDMultiply()
    {
        var left = numbers.AsSpan();
        var right = multiplyBy.AsSpan();
        var result = results.AsSpan();

        Utils.MultiplySIMD(in left, in right, ref result);

        for (var i = 0; i < result.Length; i++)
        {
            Assert.That(result[i], Is.EqualTo(left[i] * right[i]));
        }
    }
}