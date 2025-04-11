namespace AudioSync.Util.Structures;

public readonly record struct Polar(float Norm, float Phase)
{
    public Polar LogMag(float lambda) => this with { Norm = MathF.Log((lambda * Norm) + 1) };
}