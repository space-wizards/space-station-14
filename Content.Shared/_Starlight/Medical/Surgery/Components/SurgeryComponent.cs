using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
// Based on the RMC14.
// https://github.com/RMC-14/RMC-14
namespace Content.Shared.Starlight.Medical.Surgery;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedSurgerySystem))]
[EntityCategory("Surgeries")]
public sealed partial class SurgeryComponent : Component
{
    [DataField, AutoNetworkedField, Access(typeof(SharedSurgerySystem), Other = AccessPermissions.ReadWriteExecute)]
    public int Priority;

    [DataField, AutoNetworkedField]
    public List<EntProtoId> Requirement = [];

    [DataField(required: true), AutoNetworkedField]
    public List<EntProtoId> Steps = new();
}
