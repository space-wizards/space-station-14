using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Treatments.Components;

[NetworkedComponent, RegisterComponent]
public sealed class SeverityTreatmentComponent : Component
{
    [DataField("isModifier")] public bool IsModifier = false;
    [DataField("severityChange")] public FixedPoint2 SeverityChange;
}

[NetSerializable, Serializable]
public sealed class SeverityTreatmentComponentState : ComponentState
{
    public bool IsModifier;
    public FixedPoint2 SeverityChange;

    public SeverityTreatmentComponentState(bool isModifier, FixedPoint2 severityChange)
    {
        IsModifier = isModifier;
        SeverityChange = severityChange;
    }
}
