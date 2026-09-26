using Content.Shared.Polymorph;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.CosmicCult.Components.Actions;

[NetworkedComponent, RegisterComponent]
public sealed partial class CosmicActionLapseComponent : Component
{
    /// <summary>
    /// The duration of Lapse's "stasis".
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(14);

    [DataField]
    public EntProtoId SpawnLapse = "CosmicLapsedEntity";

    /// <summary>
    /// What container the entity should be trapped in.
    /// </summary>
    [DataField]
    public string Container = "entity_storage";
}

