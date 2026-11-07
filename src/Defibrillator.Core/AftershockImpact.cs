using System;

namespace Defibrillator.Core;

public static class AftershockImpact
{
    public static bool RollTumble(double sample, double chance)
    {
        if (!Settings.Finite(sample) || sample < 0 || sample >= 1) throw new ArgumentOutOfRangeException(nameof(sample));
        if (!Settings.Finite(chance) || chance < 0 || chance > 1) throw new ArgumentOutOfRangeException(nameof(chance));
        return sample < chance;
    }

    /// <summary>Cumulative horizontal turn; differences between samples are frame-rate independent.</summary>
    public static float YawAt(float degrees, double progress)
    {
        if (!Settings.Finite(degrees) || Math.Abs(degrees) > 180) throw new ArgumentOutOfRangeException(nameof(degrees));
        if (!Settings.Finite(progress)) throw new ArgumentOutOfRangeException(nameof(progress));
        double t = Math.Max(0, Math.Min(1, progress));
        return (float)(degrees * t * t * (3 - 2 * t));
    }

    public static (float Strength, float Seconds) UpgradeLegacy(int revision, float strength, float seconds) =>
        revision >= 1 ? (strength, seconds) : (strength == 3.5f ? 7f : strength, seconds == 0.8f ? 1.2f : seconds);
}
