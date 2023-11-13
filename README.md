# AudioSync

A pure C# implementation of Bram van de Wetering's paper ["Non-casual Beat Tracking for Rhythm Games"](https://github.com/nathanstep55/bpm-offset-detector/blob/main/original-paper/report.pdf). In the rhythm game community, this is more well-known as the ArrowVortex BPM detection algorithm.

## Usage

### Gather Audio Information

In your game engine or audio processing library, obtain an array containing audio samples (from -1.0 to 1.0), as well as the sample rate and number of channels in your song.

#### NAudio

```cs
using NAudio.Wave;

using var reader = new AudioFileReader("song.mp3");

var channels = reader.WaveFormat.Channels;
var sampleRate = reader.WaveFormat.SampleRate;
var sampleLength = (int)Math.Ceiling(reader.TotalTime.TotalSeconds * reader.WaveFormat.AverageBytesPerSecond);

var samples = new float[sampleLength];

reader.Read(samples, 0, sampleLength);
```

#### Unity

> [!NOTE]  
> Using AudioSync in Unity projects requires Unity **2021.2** or newer, and [API Compatibility Level](https://docs.unity3d.com/2021.2/Documentation/Manual/dotnetProfileSupport.html) set to **.NET Standard 2.1**.

```cs
using UnityEngine;

// Obtain a reference to your AudioClip somewhere
AudioClip audioClip;

var channels = audioClip.channels;
var sampleRate = audioClip.frequency;
var sampleLength = Mathf.CeilToInt(audioClip.length * audioClip.frequency);

var samples = new float[sampleLength];
audioClip.GetData(samples, 0);
```

### Run Sync Analysis

Using AudioSync, convert your samples to a mono-channel `double[]` and run through the library.

```cs

// AudioSync contains helper methods to automatically compress and convert your samples to a mono-channel double[]
var monoSamples = samples.ConvertToMonoSamples(channel);

var syncAnalysis = new SyncAnalyser();

// Synchronously
var results = syncAnalysis.Run(monoSamples, sampleRate);

// Asynchronously
var results = await SyncAnalysis.RunAsync(monoSamples, sampleRate);
```

## TODO

- Add overload for `float[]` parameters (seems Unity and NAudio all use `float[]` for storing samples)
- Build out tests to cover `AudioSync.OnsetDetection` and `AudioSync` projects
- Publish to NuGet

### Acknowledgements

- Bram van de Wetering
- [mattmora](https://github.com/mattmora)