using Content.Server.Medical.Treatments.Systems;
using Content.Shared.FixedPoint;

namespace Content.Server.Medical.Treatments.Components;

[RegisterComponent]
[Access(typeof(TreatmentSystem))]
public sealed class TreatSeverityComponent : Component
{
    [DataField("decrease"), AutoNetworkedField]
    public FixedPoint2 Decrease;
}
