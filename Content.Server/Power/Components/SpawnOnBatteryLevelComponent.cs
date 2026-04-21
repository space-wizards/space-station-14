using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server.Power.Components;

/// <summary>
/// Spawns a entity when the battery reaches a certain percentage or amount of power.
/// It also consumes that much power when spawning the entity.
/// </summary>
[RegisterComponent]
public sealed partial class SpawnOnBatteryLevelComponent : Component
{
    /// <summary>
    /// The entity table to spawn stuff from.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Table;

    /// <summary>
    /// Amount of power in the battery (in joules) to spawn entity
    /// </summary>
    [DataField]
    public float Charge;
}
