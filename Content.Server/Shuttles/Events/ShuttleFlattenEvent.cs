namespace Content.Server.Shuttles.Events;

/// <summary>
/// Raised broadcast whenever a shuttle FTLs
/// </summary>
[ByRefEvent]
public readonly record struct ShuttleFlattenEvent(EntityUid MapUid, List<Box2> AABBs);
