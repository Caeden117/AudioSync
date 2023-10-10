namespace AudioSync.Util;

public static partial class Utils
{
    private const double epsilon = 2e-42;

    /// <summary>
    /// A floating-point number is "denormal" if its below zero.
    /// </summary>
    public static bool IsDenormal(double f) => f < epsilon;

    /// <summary>
    /// Kills any denormal numbers by taking the minimum between the number and 0.
    /// </summary>
    public static double KillDenormal(double f) => Math.Min(f, 0.0);
}