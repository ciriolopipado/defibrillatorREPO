using System;
using System.Collections.Generic;
using System.Linq;
using Defibrillator.Core;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace Defibrillator.Mod;

internal static class VoiceDebuffs
{
    internal static readonly Dictionary<int, VoiceEffect> Effects = new Dictionary<int, VoiceEffect>();
    private static readonly Dictionary<string, int> seenPulses = new Dictionary<string, int>();
    private static readonly HashSet<string> endedTokens = new HashSet<string>();
    private static readonly Dictionary<string, int> seenAnimals = new Dictionary<string, int>();
    internal static bool AnimalEnabled;
    internal static double AnimalInterval;
    internal static float AnimalVolume;
    internal static double Seconds;
    internal static float HighPitch;
    internal static float LowPitch;
    internal static double ShockMinimum;
    internal static double ShockMaximum;
    internal static float ShakeStrength;
    internal static float ShakeSeconds;
    internal static float YawDegrees;
    internal static double TumbleChance;
    private static float nextSnapshotRequest;
    private static object? previousRoom;
    private static readonly System.Reflection.FieldInfo VoicePlayer = AccessTools.Field(typeof(PlayerVoiceChat), "playerAvatar")
        ?? throw new MissingFieldException("PlayerVoiceChat.playerAvatar");

    internal static double Now => SemiFunc.IsMultiplayer() ? PhotonNetwork.Time : Time.realtimeSinceStartupAsDouble;
    internal static string Round => RunManager.instance.levelCurrent.name + "/" + RunManager.instance.levelsCompleted;

    internal static void Revived(int target)
    {
        if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
        EnsureRoom();
        double firstShock = NextShockInterval();
        var effect = new VoiceEffect(VoiceEffect.ChoosePitch(UnityEngine.Random.Range(0, 2), HighPitch, LowPitch),
            Now + Seconds, Now + firstShock, firstShock, Round, Guid.NewGuid().ToString("N"),
            nextAnimalAt: Now + AnimalInterval, animalInterval: AnimalInterval);
        Effects[target] = effect;
        seenPulses[effect.Token] = 0;
        seenAnimals[effect.Token] = 0;
        Plugin.Instance.Network.SendVoice(target, effect);
        Plugin.LogInfo("Revival aftereffects applied: target=" + target + ", pitch=" + effect.Pitch + ", voiceSeconds=" + Seconds);
    }

    internal static void Tick()
    {
        EnsureRoom();
        if (!SemiFunc.RunIsLevel()) { AnimalVoicePlayback.StopAll(); Effects.Clear(); seenPulses.Clear(); seenAnimals.Clear(); endedTokens.Clear(); return; }
        foreach (var pair in Effects.ToArray())
        {
            var player = GameAccess.FindPlayer(pair.Key);
            if (!player)
            {
                // A late-joining peer may receive effect state before the player prefab.
                Effects.Remove(pair.Key);
                seenPulses.Remove(pair.Value.Token);
                continue;
            }
            if (!pair.Value.IsActive(Now, Round, GameAccess.IsAlive(player)))
            {
                ClearPlayer(pair.Key);
                continue;
            }
            if (SemiFunc.IsMasterClientOrSingleplayer() && Now >= pair.Value.NextShockAt &&
                pair.Value.TryShock(Now, Round, true, NextShockInterval()))
            {
                float yaw = YawDegrees * (UnityEngine.Random.Range(0, 2) == 0 ? -1 : 1);
                bool tumble = AftershockImpact.RollTumble(UnityEngine.Random.Range(0, 1000000) / 1000000d, TumbleChance);
                Shock(pair.Key, pair.Value.Token, pair.Value.Sequence, ShakeStrength, ShakeSeconds, yaw, tumble);
                Plugin.Instance.Network.SendShock(pair.Key, pair.Value.Token, pair.Value.Sequence, ShakeStrength, ShakeSeconds, yaw, tumble);
            }
            if (SemiFunc.IsMasterClientOrSingleplayer() && AnimalEnabled && AnimalClips.Ready && pair.Value.TryAnimal(Now, Round, true))
            {
                int animal = UnityEngine.Random.Range(0, 4);
                double at = Now;
                Animal(pair.Key, pair.Value.Token, pair.Value.AnimalSequence, animal, at);
                Plugin.Instance.Network.SendAnimal(pair.Key, pair.Value.Token, pair.Value.AnimalSequence, animal, at);
            }
        }
        // Snapshots recover late joins, delayed prefab creation and host changes.
        if (SemiFunc.IsMultiplayer() && !PhotonNetwork.IsMasterClient && Time.unscaledTime >= nextSnapshotRequest)
        {
            nextSnapshotRequest = Time.unscaledTime + 3;
            Plugin.Instance.Network.RequestVoiceSnapshot();
        }
    }

    private static double NextShockInterval() => ShockSchedule.Choose(ShockMinimum, ShockMaximum,
        UnityEngine.Random.Range(0, 1000001) / 1000000d);

    private static void EnsureRoom()
    {
        object? room = PhotonNetwork.CurrentRoom;
        if (!ReferenceEquals(room, previousRoom))
        {
            AnimalVoicePlayback.StopAll();
            Effects.Clear();
            seenPulses.Clear();
            seenAnimals.Clear();
            endedTokens.Clear();
            previousRoom = room;
            nextSnapshotRequest = 0;
        }
    }

    internal static void ClearPlayer(int target)
    {
        if (!Effects.TryGetValue(target, out var effect)) return;
        Effects.Remove(target);
        AnimalVoicePlayback.Stop(target);
        seenPulses.Remove(effect.Token);
        seenAnimals.Remove(effect.Token);
        endedTokens.Add(effect.Token);
    }

    internal static void Receive(int target, VoiceEffect effect)
    {
        EnsureRoom();
        if (!RunManager.instance || !SemiFunc.RunIsLevel() || effect.Round != Round || endedTokens.Contains(effect.Token)) return;
        if (Effects.TryGetValue(target, out var previous) && previous.Token != effect.Token) seenPulses.Remove(previous.Token);
        Effects[target] = effect;
        seenPulses[effect.Token] = Math.Max(effect.Sequence, seenPulses.TryGetValue(effect.Token, out var previousSequence) ? previousSequence : 0);
        seenAnimals[effect.Token] = Math.Max(effect.AnimalSequence, seenAnimals.TryGetValue(effect.Token, out var priorAnimal) ? priorAnimal : 0);
    }

    internal static void Animal(int target, string token, int sequence, int animal, double at)
    {
        if (!AnimalClips.Ready || animal < 0 || animal >= 4 || sequence <= 0 ||
            !Effects.TryGetValue(target, out var effect) || effect.Token != token ||
            (seenAnimals.TryGetValue(token, out var previous) && sequence <= previous)) return;
        var player = GameAccess.FindPlayer(target);
        if (!player || !effect.IsActive(Now, Round, GameAccess.IsAlive(player))) return;
        seenAnimals[token] = sequence;
        AnimalVoicePlayback.Play(target, token, animal, at);
    }

    internal static void Shock(int target, string token, int sequence, float strength, float seconds, float yaw, bool tumble)
    {
        if (!Effects.TryGetValue(target, out var effect) || effect.Token != token ||
            (seenPulses.TryGetValue(token, out var previous) && sequence <= previous)) return;
        var player = GameAccess.FindPlayer(target);
        if (!player || !effect.IsActive(Now, Round, GameAccess.IsAlive(player))) return;
        seenPulses[token] = sequence;
        AftershockVisual.Spawn(player, sequence, strength, seconds);
        AftershockCamera.Spawn(player, yaw, seconds);
        if (tumble && SemiFunc.IsMasterClientOrSingleplayer() && !GameAccess.IsTumbling(player))
        {
            var nativeTumble = GameAccess.Tumble(player);
            // Forced native Q posture, without triggering the voluntary tumble-launch upgrade.
            if (nativeTumble) nativeTumble!.TumbleSet(true, false);
        }
    }

    internal static void Apply(PlayerVoiceChat voice)
    {
        if (!Plugin.Instance || !Plugin.Instance.Operational || !RunManager.instance || !SemiFunc.RunIsLevel()) return;
        var player = (PlayerAvatar?)VoicePlayer.GetValue(voice);
        if (player && Effects.TryGetValue(GameAccess.PlayerId(player!), out var effect) &&
            effect.HasVoice(Now, Round, GameAccess.IsAlive(player!)))
            voice.OverridePitch(effect.Pitch, 0.12f, 0.2f, 0.3f);
        // Native pitch recovers when this short override stops being refreshed.
    }
}

[HarmonyPatch(typeof(PlayerVoiceChat), "Update")]
internal static class VoicePatch
{
    [HarmonyPrefix]
    private static void Prefix(PlayerVoiceChat __instance)
    {
        AnimalVoicePlayback.BeforeVoice(__instance);
        VoiceDebuffs.Apply(__instance);
    }
    [HarmonyPostfix]
    private static void Postfix(PlayerVoiceChat __instance) => AnimalVoicePlayback.AfterVoice(__instance);
}
