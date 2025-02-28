using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.CosmicCult.Components;

[RegisterComponent]
public sealed partial class CosmicGlyphTransmuteArmorComponent : Component
{
    /// <summary>
    ///     The search range for finding transmutation targets.
    /// </summary>
    [DataField] public float TransmuteRange = 0.5f;

    /// <summary>
    ///     The armor for transmuting.
    /// </summary>
    ///
    [DataField] public EntProtoId TransmuteArmor = "ClothingOuterHardsuitCosmicCult";
}
