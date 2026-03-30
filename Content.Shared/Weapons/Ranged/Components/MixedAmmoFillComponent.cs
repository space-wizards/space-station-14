using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Defines one entry in a MixedAmmoFillComponent's weighted ammo table.
/// </summary>
[DataDefinition]
public sealed partial class MixedAmmoEntry
{
    /// <summary>
    /// Prototype ID of the cartridge/shell entity to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Proto = default!;

    /// <summary>
    /// Relative weight for random selection. Higher values = more common.
    /// </summary>
    [DataField]
    public float Weight = 1f;
}

/// <summary>
/// When added to an entity with BallisticAmmoProviderComponent, fills the magazine
/// at MapInit with a weighted-random mix of ammo types in randomized order.
/// Replaces RandomAmmoFillComponent for second-hand magazines.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MixedAmmoFillComponent : Component
{
    /// <summary>
    /// Weighted pool of ammo prototypes to draw from when filling.
    /// </summary>
    [DataField]
    public List<MixedAmmoEntry> Entries = new();

    /// <summary>
    /// Lower bound of the loaded-round fraction (0.0–1.0).
    /// </summary>
    [DataField]
    public float MinFillFraction = 0.6f;

    /// <summary>
    /// Upper bound of the loaded-round fraction (0.0–1.0).
    /// </summary>
    [DataField]
    public float MaxFillFraction = 1.0f;
}
