namespace AudioSync.Util.Tests.SpanQuickSort;

[TestFixture(new double[] { 3, 2, 1 })]
[TestFixture(new double[] { 1, 3, 3, 6, 7, 8, 9 })]
[TestFixture(new double[] { 1, 2, 3, 4, 5, 6, 8, 9 })]
[TestFixture(new double[] { 8, 14, 8, 45, 1, 31, 16, 40, 12, 30, 42, 30, 24 })]
[TestFixture(new double[] { 4, 3, 7, 8, 4, 5, 12, 4, 5, 3, 2, 3 })]
[TestFixture(new double[] { 1, 3, 4, 8, 12, 13, 15, 17, 19, 20 })]
[TestFixture(new double[] { 31, 28, 19, 14, 11, 30, 27, 20 })]
[TestFixture(new double[] { 4, 5, 8, 12, 15, 17, 18 })]
[TestFixture(new double[] { 23, 26, 26, 26, 29, 36, 39, 40, 42, 42, 48, 49 })]
// Manual test fixures with numbers that I typed as fast as I could.
public class QuickSortTests
{
    private readonly double[] numbers;

    public QuickSortTests(double[] numbers) => this.numbers = numbers;

    [Test]
    public void Sort()
    {
        // I am well aware that this is a Span on the heap.
        // This should work for Spans on the stack as well, since both use the same data structure
        var span = numbers.AsSpan();

        Utils.QuickSort(ref span);

        Assert.That(numbers, Is.Ordered);
    }
}
