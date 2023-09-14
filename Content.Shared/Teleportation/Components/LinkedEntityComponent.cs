using Content.Shared.Teleportation.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Teleportation.Components;

/// <summary>
///     Represents an entity which is linked to other entities (perhaps portals), and which can be walked through/
///     thrown into to teleport an entity.
/// </summary>
[RegisterComponent, Access(typeof(LinkedEntitySystem)), NetworkedComponent]
public sealed partial class LinkedEntityComponent : Component
{
    /// <summary>
    ///     The entities that this entity is linked to.
    /// </summary>
    [DataField("linkedEntities")]
    public HashSet<EntityUid> LinkedEntities = new();

    /// <summary>
    ///     Should this entity be deleted if all of its links are removed?
    /// </summary>
    [DataField("deleteOnEmptyLinks")]
    public bool DeleteOnEmptyLinks = false;
}

[Serializable, NetSerializable]
public sealed class LinkedEntityComponentState : ComponentState
{
    public HashSet<NetEntity> LinkedEntities;

    public LinkedEntityComponentState(HashSet<NetEntity> linkedEntities)
    {
        LinkedEntities = linkedEntities;
    }
}

[Serializable, NetSerializable]
public enum LinkedEntityVisuals : byte
{
    HasAnyLinks
}
