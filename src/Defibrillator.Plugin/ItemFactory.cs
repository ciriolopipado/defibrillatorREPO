using System;
using System.Linq;
using REPOLib.Modules;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Defibrillator.Mod;

internal static class ItemFactory
{
    internal static Item? Item;
    private static bool failed;

    internal static void EnsureRegistered()
    {
        if (Item || failed || !RunManager.instance) return;
        GameObject? staging = null;
        try
        {
            // Use the game's tracker as a physics/inventory/battery shell, never redistribute it.
            var donor = Resources.LoadAll<Item>("ScriptableObjects")
                .Where(item => item && item.prefab != null && item.prefab.IsValid())
                .Where(item => item.name.IndexOf("Tracker", StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(item => item.name, StringComparer.Ordinal)
                .Select(item => (Item: item, Prefab: item.prefab.Prefab))
                .First(pair => pair.Prefab && pair.Prefab.GetComponent<ItemTracker>() &&
                    pair.Prefab.GetComponent<ItemBattery>() && pair.Prefab.GetComponent<ItemEquippable>() &&
                    pair.Prefab.GetComponentInChildren<BatteryVisualLogic>(true));

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
            Object.DestroyImmediate(prefab.GetComponent<ItemTracker>());
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
}
