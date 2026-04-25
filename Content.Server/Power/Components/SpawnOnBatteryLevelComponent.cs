using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;

namespace Content.Server.Power.Components;

/// <summary>
/// Spawns a entity when the battery reaches a certain amount of power.
/// It also consumes that much power when spawning the entity.
/// </summary>
[RegisterComponent]
public sealed partial class SpawnOnBatteryLevelComponent : Component
{
    /// <summary>
    /// A entity proto to spawn.
    /// </summary>
    /// <remarks>Takes priority over <see cref="Table"/></remarks>
    [DataField]
    public EntProtoId? Proto;

    /// <summary>
    /// A entity table to spawn stuff from.
    /// </summary>
    [DataField]
    public EntityTableSelector? Table;

    /// <summary>
    /// Amount of power in the battery (in joules) to spawn entity
    /// </summary>
    /// <remarks>It's initialized to have the value of the max battery charge if 0.0f</remarks>
    [DataField]
    public float Charge;
}
