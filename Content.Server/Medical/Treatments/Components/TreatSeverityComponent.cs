using Content.Server.Medical.Treatments.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Server.Medical.Treatments.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(TreatmentSystem))]
[AutoGenerateComponentState]
public sealed partial class TreatSeverityComponent : Component
{
    [DataField("decrease"), AutoNetworkedField]
    public FixedPoint2 Decrease;
}
