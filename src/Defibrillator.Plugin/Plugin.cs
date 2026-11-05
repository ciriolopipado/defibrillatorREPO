using System;
using BepInEx;
using Defibrillator.Core;
using HarmonyLib;
using UnityEngine;

namespace Defibrillator.Mod;

[BepInPlugin(Id, "Industrial Defibrillator", "0.1.0")]
[BepInDependency("REPOLib", "4.3.0")]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Id = "ciriolopipado.defibrillator";
    internal static Plugin Instance = null!;
    internal Settings Settings = null!;
    internal NetworkBridge Network = null!;
    private Harmony? harmony;
    private bool failed;
    private bool wasHost;
    internal bool Operational => !failed;
    internal static RevivalService? Service;
    internal static readonly GameWorld World = new GameWorld();

    private void Awake()
    {
        Instance = this;
        try
        {
            Settings = new Settings(
                Config.Bind("Revival", "HoldSeconds", 3d, "Host setting: hold duration, 0.25–30 seconds.").Value,
                Config.Bind("Revival", "RangeMeters", 2d, "Host setting: maximum eye-to-head distance, up to 10 meters.").Value,
                Config.Bind("Revival", "Health", 1, "Host setting: health after revival, 1–100.").Value,
                Config.Bind("Battery", "CostPercent", 25, "Host setting: percent spent per successful revival, 1–100.").Value,
                Config.Bind("Shop", "Price", 0, "Host setting: price in displayed shop currency units, not raw valuable dollars.").Value);
            GameAccess.Validate();
            VoiceDebuffs.Seconds = Config.Bind("VoiceDebuff", "Seconds", 180d, "Maximum duration, 1–3600 seconds; also ends at death or level end.").Value;
            VoiceDebuffs.HighPitch = Config.Bind("VoiceDebuff", "HighPitch", 1.8f, "Comically high voice multiplier, above 1 and up to 2.").Value;
            VoiceDebuffs.LowPitch = Config.Bind("VoiceDebuff", "LowPitch", 0.55f, "Comically low voice multiplier, at least 0.5 and below 1.").Value;
            VoiceDebuffs.ShockInterval = Config.Bind("Aftershock", "IntervalSeconds", 30d, "Harmless shock interval, 5–300 seconds; ends at death or level end.").Value;
            VoiceDebuffs.ShakeStrength = Config.Bind("Aftershock", "CameraShakeStrength", 3.5f, "Camera shake strength, 0–10; respects native camera-shake settings.").Value;
            VoiceDebuffs.ShakeSeconds = Config.Bind("Aftershock", "CameraShakeSeconds", 0.8f, "Camera shake duration, 0.1–2 seconds.").Value;
            _ = VoiceEffect.ChoosePitch(0, VoiceDebuffs.HighPitch, VoiceDebuffs.LowPitch);
            if (double.IsNaN(VoiceDebuffs.Seconds) || double.IsInfinity(VoiceDebuffs.Seconds) ||
                VoiceDebuffs.Seconds < 1 || VoiceDebuffs.Seconds > 3600 ||
                double.IsNaN(VoiceDebuffs.ShockInterval) || double.IsInfinity(VoiceDebuffs.ShockInterval) ||
                VoiceDebuffs.ShockInterval < 5 || VoiceDebuffs.ShockInterval > 300 ||
                float.IsNaN(VoiceDebuffs.ShakeStrength) || VoiceDebuffs.ShakeStrength < 0 || VoiceDebuffs.ShakeStrength > 10 ||
                float.IsNaN(VoiceDebuffs.ShakeSeconds) || VoiceDebuffs.ShakeSeconds < 0.1f || VoiceDebuffs.ShakeSeconds > 2)
                throw new ArgumentException("Invalid voice debuff duration settings.");
            Network = new NetworkBridge();
            harmony = new Harmony(Id);
            harmony.PatchAll(typeof(Plugin).Assembly);
            Logger.LogInfo("Industrial Defibrillator 0.1.0 loaded. Target: R.E.P.O. 0.5.1 / REPOLib 4.3.0. In-game validation required.");
        }
        catch (Exception exception)
        {
            failed = true;
            harmony?.UnpatchSelf();
            Logger.LogError("Defibrillator initialization failed; features disabled. " + exception);
        }
    }

    private void Update()
    {
        if (failed || !RunManager.instance || !GameManager.instance) return;
        try
        {
            ItemFactory.EnsureRegistered();
            VoiceDebuffs.Tick();
            bool host = SemiFunc.IsMasterClientOrSingleplayer();
            if (host != wasHost || (host && Service == null))
            {
                Service?.CancelAll();
                Service = host ? new RevivalService(Settings, World, () => Time.realtimeSinceStartupAsDouble) : null;
                wasHost = host;
                Network.ResetLocalInput();
            }
            if (!host) return;
            foreach (var device in Device.Active)
            {
                if (!device || !device.Ready) continue;
                device.SyncHostBattery();
            }
            Service?.Tick();
        }
        catch (Exception exception)
        {
            failed = true;
            Service?.CancelAll();
            Service = null;
            Logger.LogError("Defibrillator stopped after an unexpected runtime error. Restart the lobby and report this log. " + exception);
        }
    }

    private void OnDestroy()
    {
        failed = true;
        Service?.CancelAll();
        Service = null;
        harmony?.UnpatchSelf();
    }

    internal static void LogInfo(string message) => Instance.Logger.LogInfo(message);
    internal static void Error(string message) => Instance.Logger.LogError(message);
}
