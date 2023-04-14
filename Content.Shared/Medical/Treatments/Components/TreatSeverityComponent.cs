using Content.Shared.FixedPoint;
using Content.Shared.Medical.Treatments.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Treatments.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(TreatmentSystem))]
[AutoGenerateComponentState]
public sealed class TreatSeverityComponent : Component
{
    [DataField("isModifier"), AutoNetworkedField]
    public bool IsModifier = false;

    [DataField("severityChange"), AutoNetworkedField]
    public FixedPoint2 SeverityChange;
}
