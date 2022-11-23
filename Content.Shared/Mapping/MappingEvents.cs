using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Mapping;

[NetSerializable, Serializable]
public sealed class EnterMappingMode : EntityEventArgs
{
}


[NetSerializable, Serializable]
public sealed class ExitMappingMode : EntityEventArgs
{
}

[NetSerializable, Serializable]
public sealed class MappingDrawToolDrawPoint : EntityEventArgs
{
    public EntityCoordinates Point;
    public string Prototype;

    public MappingDrawToolDrawPoint(EntityCoordinates point, string prototype)
    {
        Point = point;
        Prototype = prototype;
    }
}

[NetSerializable, Serializable]
public sealed class MappingDrawToolDrawLine : EntityEventArgs
{
    public EntityCoordinates Start;
    public EntityCoordinates End;
    public string Prototype;

    public MappingDrawToolDrawLine(EntityCoordinates start, EntityCoordinates end, string prototype)
    {
        Start = start;
        End = end;
        Prototype = prototype;
    }
}

[NetSerializable, Serializable]
public sealed class MappingDrawToolDrawRect : EntityEventArgs
{
    public EntityCoordinates Start;
    public EntityCoordinates End;
    public string Prototype;

    public MappingDrawToolDrawRect(EntityCoordinates start, EntityCoordinates end, string prototype)
    {
        Start = start;
        End = end;
        Prototype = prototype;
    }
}
