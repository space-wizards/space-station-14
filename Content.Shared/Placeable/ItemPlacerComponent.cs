using Content.Shared.Whitelist;

namespace Content.Shared.Placeable;

/// <summary>
/// Detects items placed on it that match a whitelist.
/// </summary>
[RegisterComponent]
public sealed class ItemPlacerComponent : Component
{
    /// <summary>
    /// The entities that are currently on top of the placer.
    /// Guaranteed to have less than <see cref="MaxEntities"/> enitites if it is set.
    /// <summary>
    [DataField("placedEntities")]
    public HashSet<EntityUid> PlacedEntities = new();

    /// <summary>
    /// Whitelist for entities that can be placed.
    /// </summary>
    [DataField("whitelist"), ViewVariables(VVAccess.ReadWrite)]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// The max amount of entities that can be placed at the same time.
    /// If 0, there is no limit.
    /// </summary>
    [DataField("maxEntities")]
    public uint MaxEntities = 1;
}
