using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Treatments.Components;

[NetworkedComponent, RegisterComponent]
public sealed class HealTreatmentComponent : Component
{
    [DataField("fullyHeals")] public bool FullyHeals;

    [DataField("leavesScar")] public bool LeavesScar = true;

    //avoid using this when possible. Use modifier instead as it doesn't change the base healing rate.
    [DataField("baseHealingChange")] public FixedPoint2 BaseHealingChange;

    [DataField("healingModifierChange")] public FixedPoint2 HealingModifier;

    [DataField("healingMultiplierChange")] public FixedPoint2 HealingMultiplier;

}
