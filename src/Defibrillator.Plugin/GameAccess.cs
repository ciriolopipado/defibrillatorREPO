using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Defibrillator.Mod;

/// <summary>Version-sensitive access is centralized and checked during initialization.</summary>
internal static class GameAccess
{
    private static readonly FieldInfo Disabled = Require(typeof(PlayerAvatar), "isDisabled");
    private static readonly FieldInfo DeadSet = Require(typeof(PlayerAvatar), "deadSet");
    private static readonly FieldInfo DeathHead = Require(typeof(PlayerAvatar), "playerDeathHead");
    private static readonly FieldInfo Health = Require(typeof(PlayerHealth), "health");
    private static readonly FieldInfo ShopItem = Require(typeof(ItemAttributes), "shopItem");
    private static readonly FieldInfo ItemValue = Require(typeof(ItemAttributes), "value");

    private static FieldInfo Require(Type type, string field) => AccessTools.Field(type, field)
        ?? throw new MissingFieldException(type.FullName, field);

    internal static void Validate()
    {
        _ = Disabled; _ = DeathHead; _ = Health; _ = ShopItem; _ = ItemValue;
        if (AccessTools.Method(typeof(PunManager), "SpawnShopItem") == null)
            throw new MissingMethodException("PunManager.SpawnShopItem");
    }

    internal static bool IsDead(PlayerAvatar avatar) => (bool)Disabled.GetValue(avatar);
    internal static bool IsAlive(PlayerAvatar avatar) => !IsDead(avatar) && !(bool)DeadSet.GetValue(avatar);
    internal static PlayerDeathHead? Head(PlayerAvatar avatar) => (PlayerDeathHead?)DeathHead.GetValue(avatar);
    internal static int GetHealth(PlayerHealth health) => (int)Health.GetValue(health);
    internal static bool IsShopItem(ItemAttributes attributes) => (bool)ShopItem.GetValue(attributes);
    internal static void SetPrice(ItemAttributes attributes, int price) => ItemValue.SetValue(attributes, price);
    internal static int PlayerId(PlayerAvatar avatar) => SemiFunc.IsMultiplayer() ? avatar.photonView.ViewID : 1;
    internal static PlayerAvatar? FindPlayer(int id)
    {
        if (!SemiFunc.IsMultiplayer()) return id == 1 ? PlayerAvatar.instance : null;
        var view = Photon.Pun.PhotonView.Find(id);
        return view ? view.GetComponent<PlayerAvatar>() : null;
    }

    internal static bool IsLocal(PlayerAvatar avatar) => !SemiFunc.IsMultiplayer() || avatar.photonView.IsMine;
    internal static bool InputAllowed() => PlayerController.instance &&
        (float)InputDisabled.GetValue(PlayerController.instance) <= 0;
    internal static (Vector3 Position, Vector3 Forward) Aim(PlayerAvatar avatar)
    {
        if (IsLocal(avatar) && Camera.main) return (Camera.main.transform.position, Camera.main.transform.forward);
        // This is the game's replicated camera state, not a position included in a mod request.
        var position = (Vector3)CameraPosition.GetValue(avatar);
        var rotation = (Quaternion)CameraRotation.GetValue(avatar);
        return (position, rotation * Vector3.forward);
    }
    private static readonly FieldInfo CameraPosition = Require(typeof(PlayerAvatar), "clientCameraPosition");
    private static readonly FieldInfo CameraRotation = Require(typeof(PlayerAvatar), "clientCameraRotation");
    private static readonly FieldInfo InputDisabled = Require(typeof(PlayerController), "InputDisableTimer");
}
