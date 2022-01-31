using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Radar;

[Serializable, NetSerializable]
public sealed class RadarConsoleBoundInterfaceState : BoundUserInterfaceState
{
    public float Range;
    public RadarObjectData[] Objects;

    public RadarConsoleBoundInterfaceState(float range, RadarObjectData[] objects)
    {
        Range = range;
        Objects = objects;
    }
}

[Serializable, NetSerializable]
public struct RadarObjectData
{
    public Color Color;
    public RadarObjectShape Shape;
    public Vector2 Position;
    public float Radius;
}

public enum RadarObjectShape : byte
{
    Circle,
    CircleFilled,
}

[Serializable, NetSerializable]
public enum RadarConsoleUiKey : byte
{
    Key
}
