using Content.Shared.Medical.MedicalConditions.Components;

namespace Content.Shared.Medical.MedicalConditions.Systems;

[ByRefEvent]
public readonly record struct MedicalConditionAdded(EntityUid TargetEntity, EntityUid ConditionEntity,
    MedicalConditionComponent Condition, MedicalConditionReceiverComponent Receiver);

[ByRefEvent]
public readonly record struct MedicalConditionRemoved(EntityUid TargetEntity, EntityUid ConditionEntity,
    MedicalConditionComponent Condition, MedicalConditionReceiverComponent Receiver);

[ByRefEvent]
public readonly record struct MedicalConditionUpdated(EntityUid TargetEntity, EntityUid ConditionEntity,
    MedicalConditionComponent Condition, MedicalConditionReceiverComponent Receiver);
