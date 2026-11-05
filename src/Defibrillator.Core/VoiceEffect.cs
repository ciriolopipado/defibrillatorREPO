using System;

namespace Defibrillator.Core;

/// <summary>Host-selected voice and harmless periodic shock state for one revival.</summary>
public sealed class VoiceEffect
{
    public float Pitch { get; }
    public double VoiceExpiresAt { get; }
    public double NextShockAt { get; private set; }
    public double ShockInterval { get; }
    public int Sequence { get; private set; }
    public string Round { get; }
    public string Token { get; }

    public VoiceEffect(float pitch, double voiceExpiresAt, double nextShockAt,
        double shockInterval, string round, string token, int sequence = 0)
    {
        if (!Settings.Finite(pitch) || pitch < 0.5f || pitch > 2f) throw new ArgumentOutOfRangeException(nameof(pitch));
        if (!Settings.Finite(voiceExpiresAt) || voiceExpiresAt < 0) throw new ArgumentOutOfRangeException(nameof(voiceExpiresAt));
        if (!Settings.Finite(nextShockAt) || nextShockAt < 0) throw new ArgumentOutOfRangeException(nameof(nextShockAt));
        if (!Settings.Finite(shockInterval) || shockInterval < 5 || shockInterval > 300) throw new ArgumentOutOfRangeException(nameof(shockInterval));
        if (string.IsNullOrWhiteSpace(round)) throw new ArgumentException("A round identity is required.", nameof(round));
        if (string.IsNullOrWhiteSpace(token)) throw new ArgumentException("An effect identity is required.", nameof(token));
        if (sequence < 0) throw new ArgumentOutOfRangeException(nameof(sequence));
        Pitch = pitch;
        VoiceExpiresAt = voiceExpiresAt;
        NextShockAt = nextShockAt;
        ShockInterval = shockInterval;
        Round = round;
        Token = token;
        Sequence = sequence;
    }

    public bool IsActive(double now, string round, bool playerAlive) => Settings.Finite(now) && Round == round && playerAlive;
    public bool HasVoice(double now, string round, bool playerAlive) => IsActive(now, round, playerAlive) && now < VoiceExpiresAt;

    /// <summary>Host only. Missed ticks produce one pulse, never a catch-up burst.</summary>
    public bool TryShock(double now, string round, bool playerAlive)
    {
        if (!IsActive(now, round, playerAlive) || now < NextShockAt) return false;
        NextShockAt = now + ShockInterval;
        Sequence = checked(Sequence + 1);
        return true;
    }

    /// <summary>A uniform coin in {0,1} yields equal high/low probability.</summary>
    public static float ChoosePitch(int coin, float high = 1.8f, float low = 0.55f)
    {
        if (coin != 0 && coin != 1) throw new ArgumentOutOfRangeException(nameof(coin));
        if (!Settings.Finite(high) || high <= 1 || high > 2 || !Settings.Finite(low) || low < 0.5f || low >= 1)
            throw new ArgumentOutOfRangeException(nameof(high), "High must be in (1,2]; low must be in [0.5,1).");
        return coin == 0 ? high : low;
    }
}
