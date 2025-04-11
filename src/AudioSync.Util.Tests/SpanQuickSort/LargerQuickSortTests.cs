namespace AudioSync.Util.Tests.SpanQuickSort;

[TestFixture(100)]
[TestFixture(1000)]
[TestFixture(10000)]
[TestFixture(100000)]
// Testing Quick Sort at larger scales with data filled by RNG
public class LargerQuickSortTests
{
    private readonly Random rng = new();
    private readonly float[] numbers;

    public LargerQuickSortTests(int n)
    {
        numbers = new float[n];

        // Fill with RNG data
        for (var i = 0; i < n; i++)
        {
            numbers[i] = rng.Next();
        }
    }

    [Test]
    public void Sort()
    {
        var span = numbers.AsSpan();

        Utils.QuickSort(ref span);

        Assert.That(numbers, Is.Ordered);
    }
}
