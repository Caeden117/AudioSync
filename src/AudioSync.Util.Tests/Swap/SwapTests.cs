namespace AudioSync.Util.Tests.Swap;

// Test with explicitly given array
[TestFixture(new double[] { 1, 2, 3, 4 })]
[TestFixture(new double[] { 1, 2, 3, 4, 5 })]
[TestFixture(new double[] { 55.1, 234.5, 123.1, 204.2, 1, 283, 99.1 })]
// Test with elements of N size, filled with RNG values
[TestFixture(10)]
[TestFixture(100)]
[TestFixture(63)]
[TestFixture(24)]
[TestFixture(99)]
[TestFixture(17)]
[TestFixture(23)]
[TestFixture(1023)]
public class SwapTests
{
    private readonly double[] numbers;

    public SwapTests(double[] numbers) => this.numbers = numbers;

    public SwapTests(int n)
    {
        numbers = new double[n];

        // Fill with RNG data
        var rng = new Random();
        for (var i = 0; i < n; i++)
        {
            numbers[i] = rng.Next();
        }
    }

    [Test]
    public void Test()
    {
        var span = numbers.AsSpan();
        
        // Set up a copy of our data for evaluation
        Span<double> copySpan = stackalloc double[span.Length];
        span.CopyTo(copySpan);

        Utils.Swap(ref span);

        // Manually evaluate our numbers
        for (var i = 0; i < span.Length; i++)
        {
            // Calculate the IDX of our number in the swapped span.
            var swappedIdx = (i + (span.Length / 2) + (span.Length % 2)) % span.Length;

            Assert.That(numbers[i], Is.EqualTo(copySpan[swappedIdx]));
        }
    }
}
