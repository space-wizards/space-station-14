using Content.Server.StationEvents.Events;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Configuration component for the Space Ninja antag.
/// </summary>
[RegisterComponent, Access(typeof(NinjaSpawnRule))]
public sealed partial class NinjaSpawnRuleComponent : Component
{
    /// <summary>
    /// Distance that the ninja spawns from the station's half AABB radius
    /// </summary>
    [DataField("spawnDistance")]
    public float SpawnDistance = 20f;
}
