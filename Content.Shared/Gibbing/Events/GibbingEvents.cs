using Robust.Shared.Serialization;

namespace Content.Shared.Gibbing.Events;

/// <summary>
/// Called just before we actually gib the target entity
/// </summary>
/// <param name="Target">The entity being gibed</param>
/// <param name="GibletCount">how many giblets to spawn</param>
/// <param name="GibType">What type of gibbing is occuring</param>
[ByRefEvent] public record struct AttemptEntityGibEvent(EntityUid Target, int GibletCount, GibType GibType);

/// <summary>
/// Called immediately after we gib the target entity
/// </summary>
/// <param name="Target">The entity being gibbed</param>
/// <param name="DroppedEntities">Any entities that are spilled out (if any)</param>
[ByRefEvent] public record struct EntityGibbedEvent(EntityUid Target, List<EntityUid> DroppedEntities);

[Serializable, NetSerializable]
public enum GibType : byte
{
    Skip,
    Drop,
    Gib,
}

public enum GibContentsOption : byte
{
    Skip,
    Drop,
    Gib
}
