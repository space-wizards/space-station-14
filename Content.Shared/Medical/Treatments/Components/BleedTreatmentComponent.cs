using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Treatments.Components;

[NetworkedComponent, RegisterComponent]
public sealed class BleedTreatmentComponent : Component
{
    [DataField("fullyStopsBleed", required: true)]
    public bool FullyStopsBleed;

    [DataField("bleedDecrease")] public FixedPoint2 BleedDecrease = 0;
}
[Serializable, NetSerializable]
public sealed class BleedTreatmentComponentState : ComponentState
{
    public bool FullyStopsBleed;
    public FixedPoint2 BleedDecrease;

    public BleedTreatmentComponentState(bool fullyStopsBleed, FixedPoint2 bleedDecrease)
    {
        FullyStopsBleed = fullyStopsBleed;
        BleedDecrease = bleedDecrease;
    }
}
