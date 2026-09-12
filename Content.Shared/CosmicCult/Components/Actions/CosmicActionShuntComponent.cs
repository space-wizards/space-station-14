using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.CosmicCult.Components.Actions;

[NetworkedComponent, RegisterComponent]
// [AutoGenerateComponentPause]
public sealed partial class CosmicActionShuntComponent : Component
{
    /// <summary>
    /// The duration of Shunt Subjectivity's "stun", wherein the victim has their mind transferred to The Cosmic Dark as a Wisp.
    /// </summary>
    [DataField]
    public TimeSpan DurationDefault = TimeSpan.FromSeconds(22);

    [DataField]
    public TimeSpan DurationEmpowered = TimeSpan.FromSeconds(26);

    [DataField]
    public EntProtoId SpawnWisp = "MobCosmicWisp";
}
