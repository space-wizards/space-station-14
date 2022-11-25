using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Mapping;

[NetSerializable, Serializable]
public sealed class EnterMappingModeEvent : EntityEventArgs
{
}


[NetSerializable, Serializable]
public sealed class ExitMappingModeEvent : EntityEventArgs
{
}

public abstract class MappingDrawToolBaseEvent : EntityEventArgs
{
    /// <summary>
    /// The prototype being placed.
    /// </summary>
    public string Prototype;

    /// <summary>
    /// A relative "anchor" used for picking what grid to draw onto in ambiguous scenarios.
    /// </summary>
    public EntityCoordinates Anchor;
    /// <summary>
    /// The rotation of the object being placed, assuming it supports it.
    /// </summary>
    /// <remarks>To avoid a bunch of duplication this is present even when something doesn't support it at all.</remarks>
    public float Rotation;

    protected MappingDrawToolBaseEvent(string prototype, EntityCoordinates anchor, float rotation)
    {
        Prototype = prototype;
        Anchor = anchor;
        Rotation = rotation;
    }
}

[NetSerializable, Serializable]
public sealed class MappingDrawToolDrawEntityPointEvent : MappingDrawToolBaseEvent
{
    public EntityCoordinates Point;

    public MappingDrawToolDrawEntityPointEvent(string prototype, EntityCoordinates anchor, float rotation, EntityCoordinates point) : base(prototype, anchor, rotation)
    {
        Point = point;
    }
}

[NetSerializable, Serializable]
public sealed class MappingDrawToolDrawEntityLineEvent : MappingDrawToolBaseEvent
{
    public EntityCoordinates Start;
    public EntityCoordinates End;

    public MappingDrawToolDrawEntityLineEvent(string prototype, EntityCoordinates anchor, float rotation, EntityCoordinates start, EntityCoordinates end) : base(prototype, anchor, rotation)
    {
        Start = start;
        End = end;
    }
}

[NetSerializable, Serializable]
public sealed class MappingDrawToolDrawEntityRectEvent : MappingDrawToolBaseEvent
{
    public EntityCoordinates Start;
    public EntityCoordinates End;

    public MappingDrawToolDrawEntityRectEvent(string prototype, EntityCoordinates anchor, float rotation, EntityCoordinates start, EntityCoordinates end) : base(prototype, anchor, rotation)
    {
        Start = start;
        End = end;
    }
}
