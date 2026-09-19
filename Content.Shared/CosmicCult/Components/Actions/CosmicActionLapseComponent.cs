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
    public TimeSpan Duration = TimeSpan.FromSeconds(12);

    [DataField]
    public EntProtoId SpawnLapse = "CosmicLapsedEntity";
}
