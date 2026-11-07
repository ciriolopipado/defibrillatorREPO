using System;
using BepInEx;
using Defibrillator.Core;
using HarmonyLib;
using UnityEngine;

namespace Defibrillator.Mod;

[BepInPlugin(Id, "Industrial Defibrillator", Version)]
[BepInDependency("REPOLib", "4.3.0")]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Id = "ciriolopipado.defibrillator";
    public const string Version = "0.1.3";
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
            var price = Config.Bind("Shop", "Price", BalanceMigration.Price,
                "Host setting: price in displayed shop currency units; 2 means 2k.");
            var batteryCost = Config.Bind("Battery", "CostPercent", BalanceMigration.BatteryCost,
                "Host setting: percent spent per successful revival, 1–100. Default 50 gives two uses per charge.");
            var balanceRevision = Config.Bind("Internal", "BalanceRevision", 0,
                "One-time migration of legacy free-price / four-use test defaults. Keep this value after upgrading.");
            if (balanceRevision.Value < BalanceMigration.Revision)
            {
                var migrated = BalanceMigration.Apply(balanceRevision.Value, price.Value, batteryCost.Value);
                price.Value = migrated.Price;
                batteryCost.Value = migrated.BatteryCost;
                balanceRevision.Value = BalanceMigration.Revision;
                Config.Save();
                Logger.LogInfo("Balance defaults migrated: Price=" + price.Value + ", CostPercent=" + batteryCost.Value + ". Custom values were preserved.");
            }
            Settings = new Settings(
                Config.Bind("Revival", "HoldSeconds", 3d, "Host setting: hold duration, 0.25–30 seconds.").Value,
                Config.Bind("Revival", "RangeMeters", 2d, "Host setting: maximum eye-to-head distance, up to 10 meters.").Value,
                Config.Bind("Revival", "Health", 1, "Host setting: health after revival, 1–100.").Value,
                batteryCost.Value,
                price.Value);
            GameAccess.Validate();
            VoiceDebuffs.Seconds = Config.Bind("VoiceDebuff", "Seconds", 180d, "Maximum duration, 1–3600 seconds; also ends at death or level end.").Value;
            VoiceDebuffs.HighPitch = Config.Bind("VoiceDebuff", "HighPitch", 1.8f, "Comically high voice multiplier, above 1 and up to 2.").Value;
            VoiceDebuffs.LowPitch = Config.Bind("VoiceDebuff", "LowPitch", 0.55f, "Comically low voice multiplier, at least 0.5 and below 1.").Value;
            VoiceDebuffs.ShockInterval = Config.Bind("Aftershock", "IntervalSeconds", 30d, "Harmless shock interval, 5–300 seconds; ends at death or level end.").Value;
            var shakeStrength = Config.Bind("Aftershock", "CameraShakeStrength", 7f, "Camera shake strength, 0–10; respects native camera-shake settings.");
            var shakeSeconds = Config.Bind("Aftershock", "CameraShakeSeconds", 1.2f, "Camera shake duration, 0.1–2 seconds.");
            var impactRevision = Config.Bind("Internal", "AftershockRevision", 0, "Once-only upgrade of old weak shake defaults.");
            if (impactRevision.Value < 1)
            {
                var upgraded = AftershockImpact.UpgradeLegacy(impactRevision.Value, shakeStrength.Value, shakeSeconds.Value);
                shakeStrength.Value = upgraded.Strength;
                shakeSeconds.Value = upgraded.Seconds;
                impactRevision.Value = 1;
                Config.Save();
            }
            VoiceDebuffs.ShakeStrength = shakeStrength.Value;
            VoiceDebuffs.ShakeSeconds = shakeSeconds.Value;
            VoiceDebuffs.YawDegrees = Config.Bind("Aftershock", "YawDegrees", 180f, "Horizontal turn per shock, 0–180 degrees; random left/right. Set 0 to disable turning.").Value;
            VoiceDebuffs.TumbleChance = Config.Bind("Aftershock", "TumbleChance", 0.25d, "Probability of native Q/tumble per shock, 0–1; 0 disables, 1 always triggers.").Value;
            _ = AftershockImpact.YawAt(VoiceDebuffs.YawDegrees, 0);
            _ = AftershockImpact.RollTumble(0, VoiceDebuffs.TumbleChance);
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
            Logger.LogInfo("Industrial Defibrillator " + Version + " loaded. Target: R.E.P.O. 0.5.1 / REPOLib 4.3.0. In-game validation required.");
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
