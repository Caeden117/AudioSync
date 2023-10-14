namespace AudioSync.Util.Tests.Push;

[TestFixture(10)]
[TestFixture(100)]
[TestFixture(1000)]
public class PushTestMultiple
{
    private readonly int n;

    public PushTestMultiple(int n) => this.n = n;

    [Test]
    public void Push()
    {
        Span<double> numbers = stackalloc double[n];

        for (var i = 0; i < n; i++)
        {
            numbers[i] = i;
        }

        Utils.Push(ref numbers, n);

        for (var i = 0; i < n; i++)
        {
            Assert.That(numbers[i], Is.EqualTo(i + 1.0));
        }
    }

    [Test]
    public void MultiShift()
    {
        Span<double> numbers = stackalloc double[n];
        numbers.Clear();

        for (var i = 0; i < n; i++)
        {
            Utils.Push(ref numbers, i);
        }

        for (var i = 0; i < n; i++)
        {
            Assert.That(numbers[i], Is.EqualTo(i));
        }
    }
}