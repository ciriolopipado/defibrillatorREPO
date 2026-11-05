using System;

namespace Defibrillator.Core;

/// <summary>Immutable host settings. Clients must use the host's effective values.</summary>
public sealed class Settings
{
    public double HoldSeconds { get; }
    public double RangeMeters { get; }
    public int RevivalHealth { get; }
    public int BatteryCost { get; }
    public int Price { get; }
    public double HeartbeatTimeoutSeconds { get; }

    public Settings(double holdSeconds = 3, double rangeMeters = 2,
        int revivalHealth = 1, int batteryCost = 25, int price = 0,
        double heartbeatTimeoutSeconds = 0.75)
    {
        if (!Finite(holdSeconds) || holdSeconds < 0.25 || holdSeconds > 30)
            throw new ArgumentOutOfRangeException(nameof(holdSeconds));
        if (!Finite(rangeMeters) || rangeMeters <= 0 || rangeMeters > 10)
            throw new ArgumentOutOfRangeException(nameof(rangeMeters));
        if (revivalHealth < 1 || revivalHealth > 100)
            throw new ArgumentOutOfRangeException(nameof(revivalHealth));
        if (batteryCost < 1 || batteryCost > 100)
            throw new ArgumentOutOfRangeException(nameof(batteryCost));
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));
        if (!Finite(heartbeatTimeoutSeconds) || heartbeatTimeoutSeconds <= 0 ||
            heartbeatTimeoutSeconds > 2)
            throw new ArgumentOutOfRangeException(nameof(heartbeatTimeoutSeconds));
        HoldSeconds = holdSeconds;
        RangeMeters = rangeMeters;
        RevivalHealth = revivalHealth;
        BatteryCost = batteryCost;
        Price = price;
        HeartbeatTimeoutSeconds = heartbeatTimeoutSeconds;
    }

    internal static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}
