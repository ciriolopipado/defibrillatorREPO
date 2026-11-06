using System;
using System.Collections.Generic;

namespace Defibrillator.Core;

/// <summary>Original model coordinates; both art and compound collision use the same fixed scale.</summary>
public static class DeviceGeometry
{
    public const float ModelScale = 0.5f;
    public const float MassKg = 3f;
    public const float FloorY = -0.1575f * ModelScale;
    public static IReadOnlyList<CollisionBox> CollisionBoxes { get; } = Array.AsReadOnly(new[]
    {
        new CollisionBox("Casing", 0, 0, 0, 0.51f, 0.315f, 0.24f),
        new CollisionBox("Left paddle", -0.30f, 0, -0.025f, 0.10f, 0.16f, 0.12f),
        new CollisionBox("Right paddle", 0.30f, 0, -0.025f, 0.10f, 0.16f, 0.12f),
        new CollisionBox("Handle", 0, 0.23f, 0, 0.22f, 0.032f, 0.05f),
        new CollisionBox("Left handle mount", -0.11f, 0.19f, 0, 0.025f, 0.08f, 0.05f),
        new CollisionBox("Right handle mount", 0.11f, 0.19f, 0, 0.025f, 0.08f, 0.05f)
    });
}

public readonly struct CollisionBox
{
    public string Name { get; }
    public float X { get; }
    public float Y { get; }
    public float Z { get; }
    public float Width { get; }
    public float Height { get; }
    public float Depth { get; }

    internal CollisionBox(string name, float x, float y, float z, float width, float height, float depth)
    {
        Name = name; X = x; Y = y; Z = z;
        Width = width; Height = height; Depth = depth;
    }
}
