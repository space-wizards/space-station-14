using Content.Shared.FixedPoint;
using Content.Shared.Medical.Treatments.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Treatments.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(TreatmentSystem))]
[AutoGenerateComponentState]
public sealed partial class TreatBleedComponent : Component
{
    [DataField("fullyStopsBleed"), AutoNetworkedField]
    public bool FullyStopsBleed;

    [DataField("decrease"), AutoNetworkedField]
    public FixedPoint2 Decrease = 0;
}
