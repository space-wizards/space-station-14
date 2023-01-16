using Content.Shared.Medical.MedicalConditions.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Shared.Medical.MedicalConditions.Components;

[RegisterComponent, NetworkedComponent]
public sealed class MedicalConditionReceiverComponent : Component
{
    [DataField("preventedConditions", customTypeSerializer:typeof(PrototypeIdHashSetSerializer<EntityPrototype>))]
    public HashSet<string>? PreventedConditions;

    [DataField("preventedConditionGroups", customTypeSerializer:typeof(PrototypeIdHashSetSerializer<MedicalConditionGroupPrototype>))]
    public HashSet<string>? PreventedConditionGroups;
}

[Serializable, NetSerializable]
public sealed class MedicalConditionReceiverComponentState : ComponentState
{
    public HashSet<string>? PreventedConditions;
    public HashSet<string>? PreventedConditionGroups;

    public MedicalConditionReceiverComponentState(HashSet<string>? preventedConditions, HashSet<string>? preventedConditionGroups)
    {
        PreventedConditions = preventedConditions;
        PreventedConditionGroups = preventedConditionGroups;
    }
}
