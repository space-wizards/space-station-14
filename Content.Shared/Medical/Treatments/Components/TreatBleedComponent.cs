using Content.Shared.FixedPoint;
using Content.Shared.Medical.Treatments.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Treatments.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(TreatmentSystem))]
[AutoGenerateComponentState]
public sealed partial class TreatBleedComponent : Component
{
    [DataField("fullyStopsBleed", required: true), AutoNetworkedField]
    public bool FullyStopsBleed;

    [DataField("bleedDecrease"), AutoNetworkedField]
    public FixedPoint2 BleedDecrease = 0;
}
