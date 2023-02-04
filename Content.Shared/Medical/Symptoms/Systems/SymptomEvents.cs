using Content.Shared.Medical.Symptoms.Components;

namespace Content.Shared.Medical.Symptoms.Systems;

[ByRefEvent]
public readonly record struct SymptomAdded(EntityUid TargetEntity, EntityUid SymptomEntity,
    SymptomComponent Symptom, SymptomReceiverComponent Receiver);

[ByRefEvent]
public readonly record struct SymptomRemoved(EntityUid TargetEntity, EntityUid SymptomEntity,
    SymptomComponent Symptom, SymptomReceiverComponent Receiver);

[ByRefEvent]
public readonly record struct SymptomUpdated(EntityUid TargetEntity, EntityUid SymptomEntity,
    SymptomComponent Symptom, SymptomReceiverComponent Receiver);
