using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
namespace Content.Shared.Starlight.Medical.Surgery.Effects.Step;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedSurgerySystem))]
public sealed partial class SurgeryProgressComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<EntProtoId> CompletedSteps = [];

    [DataField, AutoNetworkedField]
    public HashSet<EntProtoId> CompletedSurgeries = [];

    [DataField, AutoNetworkedField]
    public HashSet<EntProtoId> StartedSurgeries = [];
}