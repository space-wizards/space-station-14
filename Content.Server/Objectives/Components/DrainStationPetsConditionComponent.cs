using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Objective condition that tracks how many of the station's named pets have been killed.
/// </summary>
[RegisterComponent, Access(typeof(DrainStationPetsConditionSystem))]
public sealed partial class DrainStationPetsConditionComponent : Component
{
    /// <summary>
    /// The pets that existed when the objective was assigned.
    /// Progress is the fraction of these that are now dead or deleted (e.g. gibbed).
    /// </summary>
    [DataField]
    public List<EntityUid> Pets = new();
}
