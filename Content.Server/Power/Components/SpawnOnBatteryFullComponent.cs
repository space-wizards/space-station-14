using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;

namespace Content.Server.Power.Components;

/// <summary>
/// Spawns a entity when the battery reaches max charge.
/// It also consumes all power when spawning the entity.
/// </summary>
[RegisterComponent]
public sealed partial class SpawnOnBatteryFullComponent : Component
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
}
