using Defibrillator.Core;
using UnityEngine;

namespace Defibrillator.Mod;

/// <summary>Only the affected player's owner changes aim; no input lock or position change.</summary>
public sealed class AftershockCamera : MonoBehaviour
{
    private PlayerAvatar target = null!;
    private float started;
    private float duration;
    private float yaw;
    private float delivered;

    internal static void Spawn(PlayerAvatar player, float degrees, float seconds)
    {
        if (!GameAccess.IsLocal(player) || degrees == 0) return;
        var root = new GameObject("Defibrillator camera jolt");
        var effect = root.AddComponent<AftershockCamera>();
        effect.target = player;
        effect.started = Time.unscaledTime;
        effect.duration = Mathf.Min(0.65f, seconds);
        effect.yaw = degrees;
    }

    private void Update()
    {
        if (!target || !GameAccess.IsLocal(target) || !GameAccess.IsAlive(target) ||
            !RunManager.instance || !SemiFunc.RunIsLevel() || !CameraAim.Instance ||
            !GameDirector.instance || GameDirector.instance.DisableInput || !GameAccess.InputAllowed())
        { Destroy(gameObject); return; }
        float progress = (Time.unscaledTime - started) / duration;
        float cumulative = AftershockImpact.YawAt(yaw, progress);
        CameraAim.Instance.AdditiveAimX(cumulative - delivered);
        delivered = cumulative;
        if (progress >= 1) Destroy(gameObject);
    }
}
