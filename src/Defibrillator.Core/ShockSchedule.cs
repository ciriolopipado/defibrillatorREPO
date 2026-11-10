using System;

namespace Defibrillator.Core;

public static class ShockSchedule
{
    public static double Choose(double minimum, double maximum, double sample)
    {
        if (!Settings.Finite(minimum) || !Settings.Finite(maximum) || minimum < 5 || maximum > 300 || maximum < minimum)
            throw new ArgumentOutOfRangeException(nameof(minimum), "Shock range must satisfy 5 <= minimum <= maximum <= 300.");
        if (!Settings.Finite(sample) || sample < 0 || sample > 1) throw new ArgumentOutOfRangeException(nameof(sample));
        return minimum + (maximum - minimum) * sample;
    }
}
