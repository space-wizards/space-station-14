using Content.Shared.FixedPoint;
using Content.Shared.Medical.Treatments;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedTreatmentSystem))]
[AutoGenerateComponentState]
public sealed class BloodPackComponent : Component
{
    [DataField("amount"), AutoNetworkedField]
    public FixedPoint2 Amount;
}
