using Content.Server.StationEvents.Events;
using Content.Shared.Storage;

namespace Content.Server.StationEvents.Components;

[RegisterComponent, Access(typeof(MarkerLocationSpawnRule))]
public sealed partial class MarkerLocationSpawnRuleComponent : Component
{
    /// <summary>
    /// Which markers will be targetted by this spawn rule, based on the given string.
    /// </summary>
    [DataField(required: true)]
    public string TargetString;

    /// <summary>
    /// The entities which should be spawned at the marker entities.
    /// </summary>
    [DataField]
    public List<EntitySpawnEntry> SpawnEntries = new();

    /// <summary>
    /// By default, this gamerule only targets a single random marker.
    /// If this is set to true, all eligible spawners will be used.
    /// </summary>
    [DataField]
    public bool TargetAllEligible = false;
}
