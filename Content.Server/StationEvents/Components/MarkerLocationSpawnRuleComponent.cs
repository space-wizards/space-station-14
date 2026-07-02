using Content.Server.StationEvents.Events;
using Content.Shared.Storage;

namespace Content.Server.StationEvents.Components;

/// <summary>
/// Spawns a selection of entities at a random marker with the corresponding <see cref="MarkerSpawnLocationComponent"/>.
/// </summary>
[RegisterComponent, Access(typeof(MarkerLocationSpawnRule))]
public sealed partial class MarkerLocationSpawnRuleComponent : Component
{
    /// <summary>
    /// Used to match which marker entities will be eligible to be selected by this spawn rule.
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
