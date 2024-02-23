using Content.Shared.Botany.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Botany.Components;

/// <summary>
/// A seed packet/clipping that can be planted in a <see cref="PlantHolderComponent"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SeedSystem))]
public sealed partial class SeedComponent : Component
{
    /// <summary>
    /// The name of this seed. Determines the name of seed packets.
    /// </summary>
    [DataField(required: true)]
    public LocId Name = string.Empty;

    /// <summary>
    /// The noun for this type of seeds. E.g. for fungi this should probably be "spores" instead of "seeds".
    /// Also used to determine the name of seed packets.
    /// </summary>
    [DataField]
    public LocId Noun = "seeds-noun-seed";

    /// <summary>
    /// Seed data used to create a new plant.
    /// </summary>
    [IncludeDataField]
    public SeedData Seed = new();
}
