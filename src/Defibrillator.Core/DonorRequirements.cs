using System;

namespace Defibrillator.Core;

[Flags]
public enum DonorParts
{
    None = 0,
    Tracker = 1,
    Attributes = 2,
    Battery = 4,
    Equippable = 8,
    Physics = 16,
    Rigidbody = 32,
    NetworkView = 64,
    ColorPreset = 128,
    BatteryDisplay = 256,
    NativeReviver = 512
}

/// <summary>Component requirements, independent of asset names and optional battery artwork.</summary>
public static class DonorRequirements
{
    public const DonorParts Required = DonorParts.Tracker | DonorParts.Attributes | DonorParts.Battery |
        DonorParts.Equippable | DonorParts.Physics | DonorParts.Rigidbody | DonorParts.NetworkView | DonorParts.ColorPreset;

    public static DonorParts Missing(DonorParts available) => Required & ~available;
    public static bool IsCompatible(DonorParts available) => Missing(available) == DonorParts.None &&
        (available & DonorParts.NativeReviver) == 0;
}
