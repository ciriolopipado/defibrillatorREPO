using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Defibrillator.Mod;

/// <summary>Positional audio and temporary native voice/animation overrides; never changes microphone capture.</summary>
public sealed class AnimalVoicePlayback : MonoBehaviour
{
    private static readonly Dictionary<int, AnimalVoicePlayback> active = new Dictionary<int, AnimalVoicePlayback>();
    private static readonly System.Reflection.FieldInfo talking = AccessTools.Field(typeof(PlayerVoiceChat), "isTalking");
    private static readonly System.Reflection.FieldInfo loudness = AccessTools.Field(typeof(PlayerVoiceChat), "clipLoudness");
    private static readonly System.Reflection.FieldInfo talkStart = AccessTools.Field(typeof(PlayerVoiceChat), "isTalkingStartTime");
    private static readonly System.Reflection.FieldInfo voiceGain = AccessTools.Field(typeof(PlayerVoiceChat), "voiceGain");
    private PlayerAvatar target = null!;
    private PlayerVoiceChat voice = null!;
    private AudioSource source = null!;
    private string token = "";
    private int id;
    private int animal;
    private double ends;
    private float started;
    private bool overridden;
    private bool nativeTalking;
    private float nativeLoudness;
    private float nativeStart;

    internal static void Play(int targetId, string effectToken, int index, double at)
    {
        if (!AnimalClips.Ready || index < 0 || index >= 4) return;
        var player = GameAccess.FindPlayer(targetId);
        var nativeVoice = player ? GameAccess.Voice(player!) : null;
        if (!player || !nativeVoice) return;
        var clip = AnimalClips.Get(index);
        double elapsed = VoiceDebuffs.Now - at;
        if (elapsed < -1 || elapsed >= clip.length) return;
        Stop(targetId);
        var root = new GameObject("Defibrillator animal voice - " + AnimalClips.Names[index]);
        var playback = root.AddComponent<AnimalVoicePlayback>();
        playback.id = targetId; playback.target = player!; playback.voice = nativeVoice!;
        playback.token = effectToken; playback.animal = index;
        playback.ends = at + clip.length; playback.started = Time.time;
        root.transform.position = nativeVoice!.transform.position;
        playback.source = root.AddComponent<AudioSource>();
        var original = nativeVoice.GetComponent<AudioSource>();
        playback.source.clip = clip;
        playback.source.pitch = 1;
        playback.source.dopplerLevel = 0;
        playback.source.playOnAwake = false;
        playback.source.loop = false;
        playback.source.spatialBlend = GameAccess.IsLocal(player!) ? 0 : original.spatialBlend;
        playback.source.rolloffMode = original.rolloffMode;
        playback.source.minDistance = original.minDistance;
        playback.source.maxDistance = original.maxDistance;
        if (original.rolloffMode == AudioRolloffMode.Custom)
            playback.source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, original.GetCustomCurve(AudioSourceCurveType.CustomRolloff));
        playback.source.outputAudioMixerGroup = nativeVoice.mixerMicrophoneSound;
        playback.source.volume = VoiceDebuffs.AnimalVolume * (GameAccess.IsLocal(player!) ? 1 : Mathf.Clamp01((float)voiceGain.GetValue(nativeVoice) * 2));
        active[targetId] = playback;
        playback.source.Play();
        playback.source.time = Mathf.Max(0, (float)elapsed);
    }

    private bool Playing => target && voice && source && source.isPlaying && VoiceDebuffs.Now < ends &&
        GameAccess.IsAlive(target) && RunManager.instance && SemiFunc.RunIsLevel() &&
        VoiceDebuffs.Effects.TryGetValue(id, out var effect) && effect.Token == token;

    private void Update()
    {
        if (!Playing) { Stop(id); return; }
        transform.position = voice.transform.position;
        source.outputAudioMixerGroup = voice.mixerMicrophoneSound;
        source.volume = VoiceDebuffs.AnimalVolume * (GameAccess.IsLocal(target) ? 1 : Mathf.Clamp01((float)voiceGain.GetValue(voice) * 2));
    }

    internal static void BeforeVoice(PlayerVoiceChat instance)
    {
        foreach (var playback in active.Values)
            if (playback.voice == instance)
            {
                playback.Restore();
                if (playback.Playing) instance.OverrideVolumeMute(0.05f);
            }
    }

    internal static void AfterVoice(PlayerVoiceChat instance)
    {
        foreach (var playback in active.Values)
            if (playback.voice == instance && playback.Playing)
            {
                playback.nativeTalking = (bool)talking.GetValue(instance);
                playback.nativeLoudness = (float)loudness.GetValue(instance);
                playback.nativeStart = (float)talkStart.GetValue(instance);
                float level = AnimalClips.Level(playback.animal, playback.source.timeSamples);
                talking.SetValue(instance, level > 0.005f);
                loudness.SetValue(instance, level);
                talkStart.SetValue(instance, playback.started);
                playback.overridden = true;
            }
    }

    private void Restore()
    {
        if (!overridden || !voice) return;
        talking.SetValue(voice, nativeTalking);
        loudness.SetValue(voice, nativeLoudness);
        talkStart.SetValue(voice, nativeStart);
        overridden = false;
    }

    internal static void Stop(int targetId)
    {
        if (!active.TryGetValue(targetId, out var playback)) return;
        active.Remove(targetId);
        playback.Restore();
        if (playback.source) playback.source.Stop();
        Destroy(playback.gameObject);
    }

    internal static void StopAll()
    {
        var ids = new List<int>(active.Keys);
        foreach (var targetId in ids) Stop(targetId);
    }

    private void OnDestroy()
    {
        Restore();
        if (active.TryGetValue(id, out var current) && current == this) active.Remove(id);
    }
}

[HarmonyPatch(typeof(PlayerVoiceChat), nameof(PlayerVoiceChat.IsTalkingRPC))]
internal static class AnimalTalkingRpcPatch
{
    [HarmonyPrefix] private static void Prefix(PlayerVoiceChat __instance) => AnimalVoicePlayback.BeforeVoice(__instance);
    [HarmonyPostfix] private static void Postfix(PlayerVoiceChat __instance) => AnimalVoicePlayback.AfterVoice(__instance);
}
