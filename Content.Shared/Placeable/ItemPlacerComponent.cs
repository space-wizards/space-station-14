using Content.Shared.Whitelist;

namespace Content.Shared.Placeable;

/// <summary>
/// Detects items placed on it that match a whitelist.
/// </summary>
[RegisterComponent]
public sealed class ItemPlacerComponent : Component
{
    /// <summary>
    /// The entities that are placed on the heater.
    /// <summary>
    [DataField("placedEntities")]
    public HashSet<EntityUid> PlacedEntities = new();

    /// <summary>
    /// Whitelist for entities that can be placed on the heater.
    /// </summary>
    [DataField("whitelist"), ViewVariables(VVAccess.ReadWrite)]
    public EntityWhitelist? Whitelist;
}
