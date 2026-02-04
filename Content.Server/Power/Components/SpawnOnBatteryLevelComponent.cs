using Robust.Shared.Prototypes;

namespace Content.Server.Power.Components;

/// <summary>
/// Spawns a entity when the battery reaches a certain percentage or amount of power.
/// It also consumes that much power when spawning the entity.
/// </summary>
[RegisterComponent]
public sealed partial class SpawnOnBatteryLevelComponent : Component
{
    /// <summary>
    /// Entity prototype to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Prototype = string.Empty;

    /// <summary>
    /// Percentage of the battery to spawn entity
    /// </summary>
    [DataField]
    public float? Level = 1f;

    /// <summary>
    /// Amount of power in the battery (in joules) to spawn entity
    /// </summary>
    [DataField]
    public float? Charge;
}
