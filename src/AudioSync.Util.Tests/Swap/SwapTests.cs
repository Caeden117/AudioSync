namespace AudioSync.Util.Tests.Swap;

// Test with explicitly given array
[TestFixture(new float[] { 1, 2 })]
[TestFixture(new float[] { 1, 2, 3 })]
[TestFixture(new float[] { 1, 2, 3, 4 })]
[TestFixture(new float[] { 1, 2, 3, 4, 5 })]
[TestFixture(new float[] { 55.1f, 234.5f, 123.1f, 204.2f, 1, 283, 99.1f })]
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
    private readonly float[] numbers;

    public SwapTests(float[] numbers) => this.numbers = numbers;

    public SwapTests(int n)
    {
        numbers = new float[n];

        // Fill with RNG data
        var rng = new Random();
        for (var i = 0; i < n; i++)
        {
            numbers[i] = rng.Next();
        }
    }

    // Test by calculating the IDX of numbers post-swap, and ensuring numbers are in their correct post-swap position.
    [Test]
    public void Test()
    {
        var span = numbers.AsSpan();
        
        // Set up a copy of our data for evaluation
        Span<float> copySpan = stackalloc float[span.Length];
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

    // Test by performing Swap twice, which should put numbers back into their original position.
    [Test]
    public void TestSwapSwap()
    {
        var span = numbers.AsSpan();

        // Set up a copy of our data for evaluation
        float[] copyArr = new float[numbers.Length];
        Array.Copy(numbers, copyArr, numbers.Length);

        // Swap twice. This should retain the original order.
        Utils.Swap(ref span);
        Utils.Swap(ref span);

        // Confirm that our post-swap array is equivalent to the pre-swap copy.
        Assert.That(numbers, Is.EquivalentTo(copyArr));
    }
}
