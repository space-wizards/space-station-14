using Content.Shared.Medical.Symptoms.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Medical.Symptoms.Components;

[RegisterComponent, NetworkedComponent]
public sealed class SymptomReceiverComponent : Component
{
    [DataField("symptomGroup", required: true, customTypeSerializer: typeof(PrototypeIdSerializer<SymptomGroupPrototype>))]
    public string? SymptomGroup;
}

[Serializable, NetSerializable]
public sealed class SymptomReceiverComponentState : ComponentState
{
    public string? SymptomGroup;

    public SymptomReceiverComponentState(string? symptomGroup)
    {
        SymptomGroup = symptomGroup;
    }
}
