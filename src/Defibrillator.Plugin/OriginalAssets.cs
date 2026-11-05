using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Defibrillator.Mod;

/// <summary>Original procedural industrial casing, paddles, cables and synthesized audio.</summary>
internal static class OriginalAssets
{
    private static AudioClip? startCharge;
    private static AudioClip? shock;
    internal static AudioClip StartCharge => startCharge ? startCharge! : startCharge = Tone("Defibrillator charge", 0.28f, false);
    internal static AudioClip Shock => shock ? shock! : shock = Tone("Defibrillator discharge", 0.45f, true);

    internal static void BuildModel(GameObject prefab)
    {
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;
        Material? template = null;
        foreach (var renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            // Preserve the game's battery display and all nonvisual native components.
            if (renderer.GetComponentInParent<BatteryVisualLogic>()) continue;
            var filter = renderer.GetComponent<MeshFilter>();
            if (filter && filter.sharedMesh)
            {
                template ??= renderer.sharedMaterial;
                var local = filter.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var point = local.center + Vector3.Scale(local.extents,
                        new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                    point = prefab.transform.InverseTransformPoint(renderer.transform.TransformPoint(point));
                    if (!hasBounds) { bounds = new Bounds(point, Vector3.zero); hasBounds = true; }
                    else bounds.Encapsulate(point);
                }
            }
            renderer.enabled = false;
        }

        var root = new GameObject("Original Industrial Defibrillator Model");
        root.layer = prefab.layer;
        root.transform.SetParent(prefab.transform, false);
        if (hasBounds && bounds.size.sqrMagnitude > 0.0001f)
        {
            root.transform.localPosition = bounds.center;
            // Fit the original model into the retained native collision shell.
            float fit = Mathf.Min(bounds.size.x / 0.68f, bounds.size.y / 0.40f, bounds.size.z / 0.28f);
            root.transform.localScale = Vector3.one * Mathf.Max(0.05f, fit);
        }
        var orange = Material(new Color(0.83f, 0.30f, 0.055f), template);
        var rubber = Material(new Color(0.055f, 0.063f, 0.067f), template);
        var metal = Material(new Color(0.58f, 0.63f, 0.65f), template);
        var screen = Material(new Color(0.02f, 0.16f, 0.15f), template);
        var white = Material(new Color(0.85f, 0.87f, 0.76f), template);
        var led = Material(new Color(0.15f, 0.95f, 0.30f), template);
        Box(root.transform, "Casing", Vector3.zero, new Vector3(0.48f, 0.28f, 0.21f), orange);
        Box(root.transform, "Lower bumper", new Vector3(0, -0.14f, 0), new Vector3(0.51f, 0.035f, 0.24f), rubber);
        Box(root.transform, "Upper bumper", new Vector3(0, 0.14f, 0), new Vector3(0.51f, 0.035f, 0.24f), rubber);
        Box(root.transform, "Display", new Vector3(-0.065f, 0.035f, -0.113f), new Vector3(0.24f, 0.12f, 0.012f), screen);
        Box(root.transform, "Cross horizontal", new Vector3(0.15f, 0.035f, -0.117f), new Vector3(0.085f, 0.023f, 0.012f), white);
        Box(root.transform, "Cross vertical", new Vector3(0.15f, 0.035f, -0.119f), new Vector3(0.023f, 0.085f, 0.012f), white);
        Box(root.transform, "Handle", new Vector3(0, 0.23f, 0), new Vector3(0.22f, 0.032f, 0.05f), rubber);
        foreach (float side in new[] { -1f, 1f })
        {
            Box(root.transform, "Handle mount", new Vector3(side * 0.11f, 0.19f, 0), new Vector3(0.025f, 0.08f, 0.05f), rubber);
            Box(root.transform, "Paddle plate", new Vector3(side * 0.30f, 0, 0), new Vector3(0.10f, 0.16f, 0.06f), metal);
            Box(root.transform, "Paddle grip", new Vector3(side * 0.30f, 0, -0.055f), new Vector3(0.045f, 0.11f, 0.055f), rubber);
            var cable = new GameObject("Paddle cable");
            cable.layer = prefab.layer;
            cable.transform.SetParent(root.transform, false);
            var line = cable.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.sharedMaterial = rubber;
            line.startWidth = line.endWidth = 0.012f;
            line.positionCount = 12;
            for (int index = 0; index < 12; index++)
            {
                float t = index / 11f;
                line.SetPosition(index, new Vector3(side * Mathf.Lerp(0.20f, 0.30f, t),
                    -0.08f - Mathf.Sin(t * Mathf.PI) * 0.15f, 0.07f));
            }
        }
        for (int index = 0; index < 4; index++)
            Box(root.transform, "Cell " + index, new Vector3(-0.16f + index * 0.055f, -0.08f, -0.12f),
                new Vector3(0.037f, 0.025f, 0.012f), led);
    }

    private static Material Material(Color color, Material? template)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = shader ? new Material(shader) : template ? new Material(template!) :
            throw new InvalidOperationException("No supported model shader available.");
        material.name = "Defibrillator original material";
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        return material;
    }

    private static void Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        Object.DestroyImmediate(part.GetComponent<Collider>());
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        part.layer = parent.gameObject.layer;
    }

    private static AudioClip Tone(string name, float duration, bool discharge)
    {
        const int rate = 44100;
        var samples = new float[(int)(rate * duration)];
        uint noise = 173;
        double phase = 0;
        for (int index = 0; index < samples.Length; index++)
        {
            double t = index / (double)rate;
            double fraction = t / duration;
            double frequency = discharge ? 180 * Math.Exp(-5 * fraction) + 65 : 600 + fraction * 1500;
            phase += 2 * Math.PI * frequency / rate;
            noise = unchecked(noise * 1664525 + 1013904223);
            double hiss = noise / (double)uint.MaxValue * 2 - 1;
            double envelope = Math.Min(1, t / 0.008) * Math.Pow(1 - fraction, discharge ? 3 : 0.7);
            samples[index] = (float)(envelope * (Math.Sin(phase) * 0.45 + (discharge ? hiss * 0.30 : 0)));
        }
        var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
