using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.MedicalConditions.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Medical.MedicalConditions.Components;

[RegisterComponent, NetworkedComponent]
public sealed class MedicalConditionComponent : Component
{
    [DataField("description", required: true)]
    public string Description = string.Empty;

    [DataField("groups", required: true,
        customTypeSerializer: typeof(PrototypeIdHashSetSerializer<MedicalConditionGroupPrototype>))]
    public HashSet<string> Groups = new();

    [DataField("alert", customTypeSerializer: typeof(PrototypeIdSerializer<AlertPrototype>))]
    public string? Alert;

    [DataField("severity")] public FixedPoint2 Severity = 0;
}

[Serializable, NetSerializable]
public sealed class MedicalConditionComponentState : ComponentState
{
    public string Description;
    public HashSet<string> Groups;
    public string? Alert;
    public FixedPoint2 Severity;

    public MedicalConditionComponentState(string description, HashSet<string> group, string? alert, FixedPoint2 severity)
    {
        Description = description;
        Groups = new HashSet<string>(group);
        Alert = alert;
        Severity = severity;
    }
}
