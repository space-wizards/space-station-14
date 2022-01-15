using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Radar;

[Serializable, NetSerializable]
public sealed class RadarConsoleBoundInterfaceState : BoundUserInterfaceState
{
    public RadarObjectData[] Objects;
    public Vector2 Position;

    public RadarConsoleBoundInterfaceState(RadarObjectData[] objects, Vector2 position)
    {
        Objects = objects;
        Position = position;
    }
}

[Serializable, NetSerializable]
public struct RadarObjectData
{
    public Color Color;
    public ObjectShape Shape;
    public Vector2 Position;
    public float Radius;
}

public enum ObjectShape
{
    Circle,
    CircleFilled,
}

[Serializable, NetSerializable]
public enum RadarConsoleUiKey
{
    Key
}
