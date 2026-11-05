using UnityEngine;
using Object = UnityEngine.Object;

namespace Defibrillator.Mod;

/// <summary>Supply native battery visual bookkeeping when a tracker has no display child.</summary>
internal static class BatteryDisplayFallback
{
    internal static BatteryVisualLogic? FindTemplate()
    {
        if (BatteryUI.instance && SelfContained(BatteryUI.instance.batteryVisualLogic))
            return BatteryUI.instance.batteryVisualLogic;
        foreach (var visual in Resources.FindObjectsOfTypeAll<BatteryVisualLogic>())
            if (SelfContained(visual)) return visual;
        return null;
    }

    private static bool SelfContained(BatteryVisualLogic visual)
    {
        if (!visual || !visual.batteryBarPrefab || !visual.barLossEffectPrefab) return false;
        // Serialized references must belong to the cloned subtree, never the live shared HUD.
        return Owns(visual, visual.batteryBarContainerGroup) && Owns(visual, visual.batteryBarContainer) &&
            Owns(visual, visual.batteryBarCharge) && Owns(visual, visual.batteryBarDrain) &&
            Owns(visual, visual.batteryBorderShadow) && Owns(visual, visual.batteryBorderMain) &&
            Owns(visual, visual.batteryBackground) && Owns(visual, visual.batteryCharge) &&
            Owns(visual, visual.batteryDrain) && visual.batteryOutVisual && visual.batteryChargeNeededVisual &&
            visual.batteryOutVisual.transform.IsChildOf(visual.transform) &&
            visual.batteryChargeNeededVisual.transform.IsChildOf(visual.transform);
    }

    private static bool Owns(BatteryVisualLogic root, Component component) => component &&
        (component.transform == root.transform || component.transform.IsChildOf(root.transform));

    internal static void Attach(GameObject prefab, BatteryVisualLogic template)
    {
        var canvasRoot = new GameObject("Defibrillator native battery bookkeeping", typeof(RectTransform), typeof(Canvas));
        canvasRoot.transform.SetParent(prefab.transform, false);
        // Custom casing LEDs and the native held-item HUD remain the visible indicators.
        // Keep this private copy non-rendering; native ItemBattery still calls its visual methods.
        canvasRoot.GetComponent<Canvas>().enabled = false;
        var copy = Object.Instantiate(template.gameObject, canvasRoot.transform);
        copy.name = "Private battery display";
        var visual = copy.GetComponent<BatteryVisualLogic>();
        visual.inUI = true;
        visual.cameraTurn = false;
        visual.itemBattery = prefab.GetComponent<ItemBattery>();
        foreach (var canvas in copy.GetComponentsInChildren<Canvas>(true)) canvas.enabled = false;
        copy.SetActive(true);
        prefab.GetComponent<ItemBattery>().hasBatteryVisuals = false;
        Plugin.LogInfo("Attached a private native battery-display fallback; the shared HUD is unchanged.");
    }
}
