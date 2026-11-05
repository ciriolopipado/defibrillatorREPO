using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace Defibrillator.Mod;

[HarmonyPatch(typeof(StatsManager), "LoadItemsFromFolder")]
internal static class RegisterPatch
{
    // REPOLib registers pending definitions after RunStartStats finishes.
    [HarmonyPrefix]
    private static void Prefix() => ItemFactory.EnsureRegistered();
}

[HarmonyPatch(typeof(ItemAttributes), nameof(ItemAttributes.GetValue))]
internal static class PricePatch
{
    [HarmonyPrefix]
    private static bool Prefix(ItemAttributes __instance)
    {
        if (!__instance.GetComponent<Device>()) return true;
        // Native GetValue clamps every item to at least one unit; bypass only for our item.
        if (SemiFunc.IsMasterClientOrSingleplayer())
        {
            int price = Plugin.Instance.Settings.Price;
            GameAccess.SetPrice(__instance, price);
            var view = __instance.GetComponent<PhotonView>();
            if (SemiFunc.IsMultiplayer() && view.ViewID != 0)
                view.RPC(nameof(ItemAttributes.GetValueRPC), RpcTarget.Others, price);
        }
        return false;
    }
}

[HarmonyPatch]
internal static class ShopPatch
{
    // Resolve the nonpublic ItemSpawn generic argument without modifying the game assembly.
    private static System.Reflection.MethodBase TargetMethod() => AccessTools.GetDeclaredMethods(typeof(PunManager))
        .Single(method => method.Name == "ShopPopulateItemVolumes" && method.GetParameters().Length == 1);

    [HarmonyPrefix, HarmonyPriority(Priority.Last)]
    private static void Prefix(PunManager __instance, object[] __args)
    {
        if (!SemiFunc.IsMasterClientOrSingleplayer() || !ShopManager.instance) return;
        ItemFactory.EnsureRegistered();
        var item = ItemFactory.Item;
        if (!item) return;
        var shop = ShopManager.instance;
        // Keep the custom definition out of random pools, including pools modified by other mods.
        shop.potentialItems.RemoveAll(candidate => candidate == item);
        shop.potentialItemConsumables.RemoveAll(candidate => candidate == item);
        shop.potentialItemUpgrades.RemoveAll(candidate => candidate == item);
        shop.potentialItemHealthPacks.RemoveAll(candidate => candidate == item);
        var volume = shop.itemVolumes.FirstOrDefault(candidate => candidate && candidate.itemVolume == item!.itemVolume)
            ?? shop.itemVolumes.FirstOrDefault(candidate => candidate);
        if (!volume)
        {
            Plugin.Error("No shop slot available for the guaranteed defibrillator.");
            return;
        }
        var originalType = volume!.itemVolume;
        try
        {
            volume.itemVolume = item!.itemVolume;
            var arguments = new object?[] { volume, new List<Item> { item }, 0, false, __args[0] };
            bool success = (bool)AccessTools.Method(typeof(PunManager), "SpawnShopItem").Invoke(__instance, arguments);
            if (!success) { Plugin.Error("Failed to reserve the defibrillator shop slot."); return; }
            // The native method will populate and delete the remaining volumes as usual.
            shop.itemVolumes.Remove(volume);
            UnityEngine.Object.Destroy(volume.gameObject);
            Plugin.LogInfo("Reserved one defibrillator shop slot at price " + Plugin.Instance.Settings.Price + ".");
        }
        catch (Exception exception) { Plugin.Error("Defibrillator shop injection failed. " + exception); }
        finally { volume.itemVolume = originalType; }
    }
}

[HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.Hurt))]
internal static class LocalDamagePatch
{
    [HarmonyPrefix]
    private static void Prefix(PlayerHealth __instance, out int __state) => __state = GameAccess.GetHealth(__instance);
    [HarmonyPostfix]
    private static void Postfix(PlayerHealth __instance, int __state)
    {
        if (GameAccess.GetHealth(__instance) < __state)
        {
            var player = __instance.GetComponentInParent<PlayerAvatar>();
            if (player) Plugin.World.Damaged(player);
        }
    }
}

[HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.UpdateHealthRPC))]
internal static class RemoteDamagePatch
{
    [HarmonyPrefix]
    private static void Prefix(PlayerHealth __instance, out int __state) => __state = GameAccess.GetHealth(__instance);
    [HarmonyPostfix]
    private static void Postfix(PlayerHealth __instance, int __state)
    {
        if (GameAccess.GetHealth(__instance) < __state)
        {
            var player = __instance.GetComponentInParent<PlayerAvatar>();
            if (player) Plugin.World.Damaged(player);
        }
    }
}

[HarmonyPatch(typeof(PlayerAvatar), "PlayerDeathDone")]
internal static class DeathPatch
{
    [HarmonyPrefix]
    private static void Prefix(PlayerAvatar __instance, out bool __state) => __state = GameAccess.IsDead(__instance);
    [HarmonyPostfix]
    private static void Postfix(PlayerAvatar __instance, bool __state)
    {
        if (!__state && GameAccess.IsDead(__instance))
        {
            Plugin.World.Died(__instance);
            VoiceDebuffs.ClearPlayer(GameAccess.PlayerId(__instance));
        }
    }
}
