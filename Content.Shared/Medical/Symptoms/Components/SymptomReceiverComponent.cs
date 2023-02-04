using Content.Shared.Medical.Symptoms.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Medical.Symptoms.Components;

[RegisterComponent, NetworkedComponent]
public sealed class SymptomReceiverComponent : Component
{
    [DataField("preventedConditions", customTypeSerializer: typeof(PrototypeIdHashSetSerializer<EntityPrototype>))]
    public HashSet<string>? PreventedConditions;

    [DataField("preventedConditionGroups",
        customTypeSerializer: typeof(PrototypeIdHashSetSerializer<SymptomGroupPrototype>))]
    public HashSet<string>? PreventedConditionGroups;
}

[Serializable, NetSerializable]
public sealed class SymptomReceiverComponentState : ComponentState
{
    public HashSet<string>? PreventedConditions;
    public HashSet<string>? PreventedConditionGroups;

    public SymptomReceiverComponentState(HashSet<string>? preventedConditions,
        HashSet<string>? preventedConditionGroups)
    {
        PreventedConditions = preventedConditions;
        PreventedConditionGroups = preventedConditionGroups;
    }
}
