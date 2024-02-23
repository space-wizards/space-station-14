using Robust.Shared.Prototypes;

namespace Content.Shared.Botany;

/// <summary>
/// Data needed to create a plant entity from a seed.
/// Uses either a default plant or a plant entity in nullspace.
/// </summary>
[DataDefinition]
public partial struct SeedData
{
    /// <summary>
    /// Plant prototype to create if using a default plant.
    /// </summary>
    [DataField]
    public EntProtoId? Plant;

    /// <summary>
    /// Plant entity to use instead of a prototype.
    /// Stored in nullspace until planted to avoid updating.
    /// </summary>
    [DataField]
    public EntityUid? Entity;
}
