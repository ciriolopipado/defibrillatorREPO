using System;
using System.Collections.Generic;
using System.Linq;
using Defibrillator.Core;
using UnityEngine;

namespace Defibrillator.Mod;

internal sealed class GameWorld : IRevivalWorld
{
    private readonly Dictionary<int, long> damage = new Dictionary<int, long>();
    private readonly Dictionary<int, long> lives = new Dictionary<int, long>();
    internal void Damaged(PlayerAvatar player)
    {
        int id = GameAccess.PlayerId(player);
        damage[id] = damage.TryGetValue(id, out var revision) ? revision + 1 : 1;
    }
    internal void Died(PlayerAvatar player)
    {
        int id = GameAccess.PlayerId(player);
        lives[id] = lives.TryGetValue(id, out var revision) ? revision + 1 : 1;
    }
    internal string Life(PlayerAvatar player)
    {
        int id = GameAccess.PlayerId(player);
        return player.GetInstanceID() + "/" + (lives.TryGetValue(id, out var revision) ? revision : 0);
    }

    public Observation Observe(int actorId, string itemId, int targetActorId)
    {
        var player = GameAccess.FindPlayer(actorId);
        var target = GameAccess.FindPlayer(targetActorId);
        var device = Device.Active.FirstOrDefault(item => item && item.Id == itemId);
        var head = target ? GameAccess.Head(target) : null;
        if (!player || !target || !head || !device || !device.Ready)
            return new Observation(false, false, false, false, false, 999, 0, "");
        var aim = GameAccess.Aim(player);
        Vector3 delta = head!.transform.position - aim.Position;
        float along = Vector3.Dot(delta, aim.Forward);
        bool aimed = along >= 0 && (delta - aim.Forward * along).magnitude <= 0.35f;
        bool visible = aimed && ClearRay(aim.Position, head.transform.position, player, head, device!);
        bool inRound = RunManager.instance && SemiFunc.RunIsLevel() && GameDirector.instance &&
            GameDirector.instance.currentState == GameDirector.gameState.Main;
        return new Observation(inRound && !GameAccess.IsShopItem(device!.Attributes),
            GameAccess.IsAlive(player), device.IsHeldBy(player),
            GameAccess.IsDead(target) && head.gameObject.activeInHierarchy, visible,
            delta.magnitude, damage.TryGetValue(actorId, out var revision) ? revision : 0, Life(target));
    }

    private static bool ClearRay(Vector3 origin, Vector3 destination, PlayerAvatar player, PlayerDeathHead head, Device device)
    {
        var ray = destination - origin;
        foreach (var hit in Physics.RaycastAll(origin, ray.normalized, ray.magnitude, ~0, QueryTriggerInteraction.Ignore))
        {
            var transform = hit.collider.transform;
            if (transform.IsChildOf(player.transform) || transform.IsChildOf(device.transform) ||
                transform.IsChildOf(head.transform)) continue;
            return false;
        }
        return true;
    }

    public bool TryRevive(int actorId, int targetActorId, string targetLifeId, int health)
    {
        if (!SemiFunc.IsMasterClientOrSingleplayer()) return false;
        var player = GameAccess.FindPlayer(targetActorId);
        if (!player || !GameAccess.IsDead(player) || Life(player) != targetLifeId) return false;
        var head = GameAccess.Head(player);
        if (!head || !head!.gameObject.activeInHierarchy) return false;
        // Native Revive grants one HP on the owning client and synchronizes the death head.
        player.Revive();
        if (GameAccess.IsDead(player)) return false;
        if (health > 1)
        {
            try { player.playerHealth.HealOther(health - 1, effect: true); }
            catch (Exception exception)
            {
                // The native revival already succeeded; still commit battery and duplicate protection.
                Plugin.Error("Revival completed, but the additional configured healing failed. " + exception);
            }
        }
        return true;
    }
}
