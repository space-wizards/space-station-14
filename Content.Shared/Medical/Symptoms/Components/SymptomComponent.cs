using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Symptoms.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Medical.Symptoms.Components;

[RegisterComponent, NetworkedComponent]
public sealed class SymptomComponent : Component
{
    [DataField("alert", customTypeSerializer: typeof(PrototypeIdSerializer<AlertPrototype>))]
    public string? Alert;

    [DataField("severity")] public FixedPoint2 Severity = 0;
}

[Serializable, NetSerializable]
public sealed class SymptomComponentState : ComponentState
{
    public string? Alert;
    public FixedPoint2 Severity;

    public SymptomComponentState(string? alert, FixedPoint2 severity)
    {
        Alert = alert;
        Severity = severity;
    }
}
