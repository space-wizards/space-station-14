using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.CosmicCult.Components;

[RegisterComponent]
public sealed partial class CosmicGlyphTransmuteWeaponComponent : Component
{
    /// <summary>
    ///     The search range for finding transmutation targets.
    /// </summary>
    [DataField] public float TransmuteRange = 0.5f;

    /// <summary>
    ///     A pool of weapons that we pick from when transmuting.
    /// </summary>
    [DataField]
    public HashSet<EntProtoId> TransmuteWeapon =
    [
        "SwordCosmicCult",
        "SpearCosmicCult",
    ];
}
