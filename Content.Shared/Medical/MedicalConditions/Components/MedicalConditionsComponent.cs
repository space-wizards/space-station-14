using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.MedicalConditions.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Medical.MedicalConditions.Components;

[RegisterComponent, NetworkedComponent]
public sealed class MedicalConditionComponent : Component
{
    [DataField("description", required: true)]
    public string Description = string.Empty;

    [DataField("group", required:true, customTypeSerializer:typeof(PrototypeIdSerializer<MedicalConditionGroupPrototype>))]
    public string Group = string.Empty;

    [DataField("alert", customTypeSerializer:typeof(PrototypeIdSerializer<AlertPrototype>))]
    public string? Alert;

    [DataField("severity")] public FixedPoint2 Severity = 0;
}

[Serializable, NetSerializable]
public sealed class MedicalConditionComponentState : ComponentState
{
    public string Description;
    public string Group;
    public string? Alert;
    public FixedPoint2 Severity;

    public MedicalConditionComponentState(string description, string group, string? alert, FixedPoint2 severity)
    {
        Description = description;
        Group = group;
        Alert = alert;
        Severity = severity;
    }

}
