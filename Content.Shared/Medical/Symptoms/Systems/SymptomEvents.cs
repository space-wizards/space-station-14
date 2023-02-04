using Content.Shared.Medical.Symptoms.Components;

namespace Content.Shared.Medical.Symptoms.Systems;

[ByRefEvent]
public readonly record struct SymptomAdded(EntityUid TargetEntity, EntityUid ConditionEntity,
    SymptomComponent Condition, SymptomReceiverComponent Receiver);

[ByRefEvent]
public readonly record struct SymptomRemoved(EntityUid TargetEntity, EntityUid ConditionEntity,
    SymptomComponent Condition, SymptomReceiverComponent Receiver);

[ByRefEvent]
public readonly record struct SymptomUpdated(EntityUid TargetEntity, EntityUid ConditionEntity,
    SymptomComponent Condition, SymptomReceiverComponent Receiver);
