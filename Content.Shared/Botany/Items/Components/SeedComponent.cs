using Content.Shared.Botany.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Botany.Items.Components;

/// <summary>
/// Data container for plant seed. Contains all info (values for components) for new plant to grow from seed.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(BotanySystem))]
public sealed partial class SeedComponent : Component
{
    /// <summary>
    /// Name of a base plant prototype to spawn.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId PlantProtoId;

    /// <summary>
    /// Serialized snapshot of plant components used to override defaults when planting.
    /// </summary>
    [DataField]
    public ComponentRegistry? PlantData;

    /// <summary>
    /// If not null, overrides the plant's initial health. Otherwise, the plant's initial health is set to the Endurance value.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float? HealthOverride;
}
