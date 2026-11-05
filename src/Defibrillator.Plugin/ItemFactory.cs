using System;
using System.Linq;
using System.Collections.Generic;
using Defibrillator.Core;
using Photon.Pun;
using REPOLib.Modules;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Defibrillator.Mod;

internal static class ItemFactory
{
    internal static Item? Item;
    private static bool failed;
    private static float nextDiscovery;
    private static string lastDiscoveryReport = "";

    internal static void EnsureRegistered()
    {
        if (Item || failed || !RunManager.instance || !StatsManager.instance) return;
        if (Time.realtimeSinceStartup < nextDiscovery) return;
        nextDiscovery = Time.realtimeSinceStartup + 2;
        GameObject? staging = null;
        try
        {
            var discovered = FindDonor();
            if (!discovered.HasValue) return;
            var donor = discovered.Value;
            BatteryVisualLogic? displayTemplate = null;
            if ((donor.Parts & DonorParts.BatteryDisplay) == 0)
            {
                displayTemplate = BatteryDisplayFallback.FindTemplate();
                if (!displayTemplate)
                {
                    ReportDiscovery("Compatible tracker found: " + donor.Item.name +
                        "; waiting for a self-contained native battery-display template. Enter a lobby/level so the HUD can initialize.");
                    return;
                }
            }
            Plugin.LogInfo("Native donor selected: " + donor.Item.name + "; components=" + donor.Parts);

            staging = new GameObject("Defibrillator Prefab Storage");
            staging.SetActive(false);
            Object.DontDestroyOnLoad(staging);
            // Inactive parent prevents native Awake/Start before the clone is configured.
            var prefab = Object.Instantiate(donor.Prefab, staging.transform);
            prefab.name = "Item Industrial Defibrillator";
            var attributes = prefab.GetComponent<ItemAttributes>();
            var definition = Object.Instantiate(donor.Item);
            definition.name = prefab.name;
            definition.itemName = "Industrial Defibrillator";
            definition.itemNameLocalized = null;
            definition.description = "Hold Interact while aiming at a dead player's head. Recharge at a charging station.";
            definition.status = Item.Status.Finalized;
            definition.disabled = false;
            definition.maxAmount = 100;
            definition.maxAmountInShop = 1;
            definition.maxPurchase = false;
            definition.minPlayerCount = 1;
            definition.itemSecretShopType = SemiFunc.itemSecretShopType.none;
            definition.value = ScriptableObject.CreateInstance<Value>();
            definition.value.valueMin = definition.value.valueMax = Plugin.Instance.Settings.Price * 1000f;
            attributes.item = definition;
            attributes.icon = null;
            foreach (var tracker in prefab.GetComponentsInChildren<ItemTracker>(true)) Object.DestroyImmediate(tracker);
            var toggle = prefab.GetComponent<ItemToggle>();
            if (toggle)
            {
                toggle.enabled = false;
                toggle.toggleState = false;
                toggle.playSound = false;
                toggle.onToggle = new UnityEvent();
            }
            var battery = prefab.GetComponent<ItemBattery>();
            battery.autoDrain = false;
            battery.batteryActive = false;
            battery.isUnchargable = false;
            if (displayTemplate) BatteryDisplayFallback.Attach(prefab, displayTemplate!);
            // Retain the native six-bar display layout; consumption is an exact percentage.
            foreach (var light in prefab.GetComponentsInChildren<Light>(true)) light.enabled = false;
            foreach (var text in prefab.GetComponentsInChildren<TMPro.TMP_Text>(true)) text.enabled = false;
            OriginalAssets.BuildModel(prefab);
            prefab.AddComponent<Device>();
            prefab.SetActive(true);
            if (Items.RegisterItem(attributes) == null) throw new InvalidOperationException("REPOLib rejected the item prefab.");
            Item = definition;
            Plugin.LogInfo("Registered Industrial Defibrillator using native shell: " + donor.Item.name);
        }
        catch (Exception exception)
        {
            failed = true;
            if (staging) Object.Destroy(staging);
            Plugin.Error("Item registration failed; no replacement of vanilla items was attempted. " + exception);
        }
    }

    private static (Item Item, GameObject Prefab, DonorParts Parts)? FindDonor()
    {
        string folder = StatsManager.instance.folderPath;
        var definitions = Resources.LoadAll<Item>(folder)
            .Concat(Resources.FindObjectsOfTypeAll<Item>())
            .Concat(StatsManager.instance.itemDictionary.Values)
            .Where(item => item && item.prefab != null && item.prefab.IsValid() && item.name != "Item Industrial Defibrillator")
            .Distinct()
            .OrderBy(item => item.name.IndexOf("Tracker", StringComparison.OrdinalIgnoreCase) >= 0 ? 0 : 1)
            .ThenBy(item => item.name, StringComparer.Ordinal);
        var report = new List<string>();
        (Item Item, GameObject Prefab, DonorParts Parts)? fallback = null;
        int examined = 0;
        foreach (var definition in definitions)
        {
            examined++;
            try
            {
                var prefab = definition.prefab.Prefab;
                if (!prefab) { report.Add(definition.name + ": prefab unavailable"); continue; }
                var parts = Parts(definition, prefab);
                if (DonorRequirements.IsCompatible(parts))
                {
                    // A display is optional for donor identity, but native battery code still needs
                    // a safe private display instance. Prefer donors that already contain one.
                    if ((parts & DonorParts.BatteryDisplay) != 0) return (definition, prefab, parts);
                    fallback ??= (definition, prefab, parts);
                }
                if ((parts & DonorParts.Tracker) != 0 || definition.name.IndexOf("Tracker", StringComparison.OrdinalIgnoreCase) >= 0)
                    report.Add(definition.name + ": present=" + parts + "; missing=" + DonorRequirements.Missing(parts));
            }
            catch (Exception exception)
            {
                report.Add(definition.name + ": load failed: " + exception.GetBaseException().Message);
            }
        }
        if (fallback.HasValue) return fallback;
        ReportDiscovery("No compatible tracker prefab is available yet. Folder=" + folder + "; definitions examined=" +
            examined + ". Candidate report: " + (report.Count == 0 ? "no tracker components found" : string.Join(" | ", report)));
        return null;
    }

    private static DonorParts Parts(Item definition, GameObject prefab)
    {
        var parts = DonorParts.None;
        if (prefab.GetComponentInChildren<ItemTracker>(true)) parts |= DonorParts.Tracker;
        if (prefab.GetComponent<ItemAttributes>()) parts |= DonorParts.Attributes;
        if (prefab.GetComponent<ItemBattery>()) parts |= DonorParts.Battery;
        if (prefab.GetComponent<ItemEquippable>()) parts |= DonorParts.Equippable;
        if (prefab.GetComponent<PhysGrabObject>()) parts |= DonorParts.Physics;
        if (prefab.GetComponent<Rigidbody>()) parts |= DonorParts.Rigidbody;
        if (prefab.GetComponent<PhotonView>()) parts |= DonorParts.NetworkView;
        if (definition.colorPreset) parts |= DonorParts.ColorPreset;
        if (prefab.GetComponentInChildren<BatteryVisualLogic>(true)) parts |= DonorParts.BatteryDisplay;
        if (prefab.GetComponentInChildren<ItemReviveItem>(true)) parts |= DonorParts.NativeReviver;
        return parts;
    }

    private static void ReportDiscovery(string report)
    {
        if (report == lastDiscoveryReport) return;
        lastDiscoveryReport = report;
        Plugin.LogInfo("Donor discovery pending: " + report);
    }

}
