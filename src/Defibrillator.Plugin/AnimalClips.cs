using System;
using System.IO;
using System.Reflection;
using Defibrillator.Core;
using UnityEngine;

namespace Defibrillator.Mod;

internal static class AnimalClips
{
    internal static readonly string[] Names = { "sheep", "cat", "dog", "horse" };
    private static readonly AudioClip[] clips = new AudioClip[4];
    private static readonly PcmWave[] data = new PcmWave[4];
    internal static bool Ready { get; private set; }

    internal static void Load()
    {
        try
        {
            for (int i = 0; i < Names.Length; i++)
            {
                using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Defibrillator.Animals." + Names[i] + ".wav")
                    ?? throw new InvalidDataException("Missing embedded animal clip: " + Names[i]);
                data[i] = PcmWave.Read(resource);
                clips[i] = AudioClip.Create("Defibrillator " + Names[i], data[i].Samples.Length, 1, data[i].SampleRate, false);
                if (!clips[i].SetData(data[i].Samples, 0)) throw new InvalidDataException("Could not load animal PCM data.");
            }
            Ready = true;
            Plugin.LogInfo("Loaded four embedded animal recordings, each at most three seconds.");
        }
        catch (Exception error)
        {
            Ready = false;
            foreach (var clip in clips) if (clip) UnityEngine.Object.Destroy(clip);
            Plugin.Error("Animal audio disabled; normal voice is preserved. " + error);
        }
    }

    internal static AudioClip Get(int index) => clips[index];
    internal static float Level(int index, int sample)
    {
        var samples = data[index].Samples;
        double sum = 0;
        int start = Math.Max(0, Math.Min(sample, samples.Length - 1));
        int count = Math.Min(512, samples.Length - start);
        for (int i = start; i < start + count; i++) sum += samples[i] * samples[i];
        return Mathf.Clamp((float)Math.Sqrt(sum / count) * 2, 0, 0.25f);
    }
}
