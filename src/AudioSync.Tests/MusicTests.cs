using AudioSync.Util;
using NAudio.Wave;

namespace AudioSync.Tests;

[TestFixture("Music\\Creo\\Place on Fire.mp3", 70)] // Creo - Place on Fire @ 70 BPM
[TestFixture("Music\\Creo\\Rock Thing.mp3", 114.0)] // Creo - Rock Thing @ 114 BPM
[TestFixture("Music\\Creo\\Showdown.mp3", 110.0)] // Creo - Showdown @ 110 BPM
[TestFixture("Music\\Creo\\Ahead of the Curve.mp3", 96.0)] // Creo - Rock Thing @ 96 BPM
[TestFixture("Music\\Creo\\Sphere.mp3", 100.0)] // Creo - Sphere @ 100 BPM
[TestFixture("Music\\Creo\\Dimension.mp3", 115.0)] // Creo - Dimension @ 115 BPM
[TestFixture("Music\\Creo\\322.mp3", 128.0)] // Creo - 322 @ 128 BPM
public class MusicTests
{
    private readonly SyncAnalyser syncAnalyser;
    private readonly string song;
    private readonly double expectedBPM;

    public MusicTests(string song, double expectedBPM)
    {
        syncAnalyser = new();

        this.song = song;
        this.expectedBPM = expectedBPM;
    }

    [Test]
    public void CalculateSongBPM()
    {
        // Read mono samples
        var reader = new AudioFileReader(song);
        var channels = reader.WaveFormat.Channels;
        var sampleRate = reader.WaveFormat.SampleRate;
        var sampleLength = (int)Math.Ceiling(reader.TotalTime.TotalSeconds * sampleRate);

        var samples = new float[sampleLength];

        reader.Read(samples, 0, sampleLength);

        var doubleSamples = samples.ConvertToMonoSamples(channels);

        // Run AudioSync
        var results = syncAnalyser.Run(doubleSamples, reader.WaveFormat.SampleRate);

        // Compare against expected BPM (it should be the first result)
        //   AudioSync often gives double time results, and thats OK too.
        Assert.That(results[0].BPM, Is.EqualTo(expectedBPM).Within(0.01)
            .Or.EqualTo(expectedBPM * 2).Within(0.01));
    }
}
