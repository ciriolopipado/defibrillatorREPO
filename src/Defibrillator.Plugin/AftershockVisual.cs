using UnityEngine;

namespace Defibrillator.Mod;

/// <summary>Purely audiovisual. No health, physics impulses, stun or damage APIs are called.</summary>
public sealed class AftershockVisual : MonoBehaviour
{
    private PlayerAvatar target = null!;
    private readonly LineRenderer[] arcs = new LineRenderer[7];
    private float ends;
    private int seed;
    private Material material = null!;

    internal static void Spawn(PlayerAvatar player, int sequence, float strength, float seconds)
    {
        if (GameAccess.IsLocal(player) && GameDirector.instance && GameDirector.instance.CameraShake)
            GameDirector.instance.CameraShake.Shake(strength, seconds, 0f);
        var root = new GameObject("Defibrillator harmless aftershock");
        var effect = root.AddComponent<AftershockVisual>();
        effect.target = player;
        effect.ends = Time.unscaledTime + seconds;
        effect.seed = sequence;
        var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Standard");
        if (!shader)
        {
            Plugin.Error("No aftershock shader found; camera shake applied but visible arcs unavailable.");
            Destroy(root);
            return;
        }
        effect.material = new Material(shader);
        effect.material.color = new Color(0.3f, 0.8f, 1f, 1f);
        for (int index = 0; index < effect.arcs.Length; index++)
        {
            var child = new GameObject("Electrical arc");
            child.transform.SetParent(root.transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.sharedMaterial = effect.material;
            line.positionCount = 9;
            line.startWidth = 0.035f;
            line.endWidth = 0.018f;
            effect.arcs[index] = line;
        }
        var audio = root.AddComponent<AudioSource>();
        audio.spatialBlend = 1;
        audio.rolloffMode = AudioRolloffMode.Linear;
        audio.minDistance = 1;
        audio.maxDistance = 14;
        audio.volume = 0.45f;
        audio.PlayOneShot(OriginalAssets.Shock);
    }

    private void Update()
    {
        if (!target || !GameAccess.IsAlive(target) || !RunManager.instance ||
            !SemiFunc.RunIsLevel() || Time.unscaledTime >= ends)
        { Destroy(gameObject); return; }
        transform.position = target.transform.position + Vector3.up * 0.3f;
        for (int index = 0; index < arcs.Length; index++)
        {
            if (!arcs[index]) continue;
            for (int point = 0; point < 9; point++)
            {
                float t = point / 8f;
                float angle = index * Mathf.PI * 2 / arcs.Length + t * 2 + seed;
                float flicker = Mathf.Sin(Time.unscaledTime * 83 + point * 5 + index * 11) * 0.12f;
                arcs[index].SetPosition(point, new Vector3(Mathf.Cos(angle) * (0.40f + flicker),
                    Mathf.Lerp(-0.35f, 0.65f, t), Mathf.Sin(angle) * (0.40f + flicker)));
            }
        }
    }

    private void OnDestroy()
    {
        if (material) Destroy(material);
    }
}
