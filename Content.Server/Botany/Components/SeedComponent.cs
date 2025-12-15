using Content.Server.Botany.Systems;
using Content.Shared.Botany.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Botany.Components;

/// <summary>
/// Data container for plant seed. Contains all info (values for components) for new plant to grow from seed.
/// </summary>
[RegisterComponent]
[Access(typeof(BotanySystem), typeof(PlantHolderSystem))]
public sealed partial class SeedComponent : SharedSeedComponent
{
    /// <summary>
    /// Name of a base plant prototype to spawn.
    /// </summary>
    [DataField]
    public EntProtoId? PlantProtoId;

    /// <summary>
    /// Serialized snapshot of plant components used to override defaults when planting.
    /// </summary>
    [DataField]
    public ComponentRegistry? PlantData;

    /// <summary>
    /// If not null, overrides the plant's initial health. Otherwise, the plant's initial health is set to the Endurance value.
    /// </summary>
    [DataField]
    public float? HealthOverride = null;
}
