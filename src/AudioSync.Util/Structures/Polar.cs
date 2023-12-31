﻿namespace AudioSync.Util.Structures;

public readonly record struct Polar(double Norm, double Phase)
{
    public Polar LogMag(double lambda) => this with { Norm = Math.Log((lambda * Norm) + 1) };
}