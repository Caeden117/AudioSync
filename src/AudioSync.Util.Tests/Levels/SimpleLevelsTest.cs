namespace AudioSync.Util.Tests.Levels;

public class SimpleLevelsTest
{
    private const double silenceThreshold = -79.9;

    [Test]
    public void CompleteSilenceIsSilent()
    {
        // 0.0 for each sample, representing completely silent audio.
        Span<double> silence = stackalloc double[100];
        silence.Clear();

        var isSilence = Utils.IsSilence(in silence, silenceThreshold);

        Assert.That(isSilence, Is.True);
    }

    [Test]
    public void CompleteSilenceIs80Decibles()
    {
        // 0.0 for each sample, representing completely silent audio.
        Span<double> silence = stackalloc double[100];
        silence.Clear();

        var soundPressure = Utils.DBSoundPressureLevel(in silence);

        Assert.That(soundPressure, Is.EqualTo(-80.0));
    }

    [Test]
    public void CompleteSilenceHas0Level()
    {
        // 0.0 for each sample, representing completely silent audio.
        Span<double> silence = stackalloc double[100];
        silence.Clear();

        var level = Utils.LevelLinear(in silence);

        Assert.That(level, Is.EqualTo(0.0));
    }

    [Test]
    public void LoudIsNotSilent()
    {
        // 1.0 for each sample, representing L O U D audio.
        Span<double> loud = stackalloc double[100];
        loud.Fill(1.0);

        var isSilence = Utils.IsSilence(in loud, silenceThreshold);

        Assert.That(isSilence, Is.False);
    }

    [Test]
    public void LoudIs0Decibles()
    {
        // 1.0 for each sample, representing L O U D audio.
        Span<double> loud = stackalloc double[100];
        loud.Fill(1.0);

        var soundPressure = Utils.DBSoundPressureLevel(in loud);

        Assert.That(soundPressure, Is.EqualTo(0.0));
    }

    [Test]
    public void LoudLevelIsEqualToOne()
    {
        // 1.0 for each sample, representing L O U D audio.
        Span<double> loud = stackalloc double[100];
        loud.Fill(1.0);

        var level = Utils.LevelLinear(in loud);

        Assert.That(level, Is.EqualTo(1.0));
    }
}
