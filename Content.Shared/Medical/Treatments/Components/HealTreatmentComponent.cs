using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

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

[Serializable, NetSerializable]
public sealed class HealTreatmentComponentState : ComponentState
{
    public bool FullyHeals;
    public bool LeavesScar;
    public FixedPoint2 BaseHealingChange;
    public FixedPoint2 HealingModifier;
    public FixedPoint2 HealingMultiplier;

    public HealTreatmentComponentState(bool fullyHeals, bool leavesScar, FixedPoint2 baseHealingChange, FixedPoint2 healingModifier, FixedPoint2 healingMultiplier)
    {
        FullyHeals = fullyHeals;
        LeavesScar = leavesScar;
        BaseHealingChange = baseHealingChange;
        HealingModifier = healingModifier;
        HealingMultiplier = healingMultiplier;
    }
}
