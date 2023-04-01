using Content.Shared.Medical.Treatments.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Medical.Treatments.Components;

[NetworkedComponent, RegisterComponent]
public sealed class TreatmentComponent : Component
{
    [DataField("treatmentType", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<TreatmentTypePrototype>))]
    public string TreatmentType = "";

    [DataField("limitedUses")] public bool LimitedUses = true;

    [DataField("uses")] public int Uses = 1;

    [DataField("selfUsable")] public bool SelfUsable = true;

    [DataField("targetUsable")] public bool TargetUsable = true;
}
[NetSerializable]
public sealed class TreatmentComponentState : ComponentState
{
    public string TreatmentType;
    public bool LimitedUses;
    public int Uses;
    public bool SelfUsable;
    public bool TargetUsable;
    public TreatmentComponentState(string treatmentType, bool limitedUses, int uses, bool selfUsable, bool targetUsable)
    {
        TreatmentType = treatmentType;
        LimitedUses = limitedUses;
        Uses = uses;
        SelfUsable = selfUsable;
        TargetUsable = targetUsable;
    }
}
