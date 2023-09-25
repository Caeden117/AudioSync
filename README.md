# AudioSync

A C# implementation of Bram van de Wetering's paper "Non-casual Beat Tracking for Rhythm Games". In the rhythm game community, this is more well-known as the ArrowVortex BPM detection algorithm.

## Why?

ArrowVortex's BPM detection algorithm is written in native C/C++, and is closed source. An open-source implementation of van de Wetering's paper is available [here](https://github.com/nathanstep55/bpm-offset-detector), but is also in native C++, and is licensed under GPL due to a dependency on [aubio](https://aubio.org/).

AudioSync aims to be a pure C# implementation of Bram va de Wetering's famous BPM detection algorithm, written with performance and design in mind, while also remaining free and open source under the MIT license.