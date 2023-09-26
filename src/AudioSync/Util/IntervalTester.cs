using AudioSync.Structures;

namespace AudioSync.Util;

internal sealed class IntervalTester
{
    public readonly int MinInterval;
    public readonly int MaxInterval;
    public readonly int NumIntervals;

    private List<Onset> onsets;

}
