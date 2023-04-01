using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Treatments.Components;

[NetworkedComponent, RegisterComponent]
public sealed class BleedTreatmentComponent : Component
{
    [DataField("fullyStopsBleed", required: true)]
    public bool FullyStopsBleed;

    [DataField("bleedDecrease")] public FixedPoint2 BleedDecrease = 0;
}
