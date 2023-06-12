using Content.Shared.Body.Part;
using Content.Shared.Medical.Wounds.Components;

namespace Content.Server.Medical.Treatments.Systems;

[ByRefEvent]
public readonly record struct WoundTreatedEvent(EntityUid WoundableId, EntityUid WoundId, WoundableComponent Woundable, WoundComponent Wound, BodyPartComponent Part);

[ByRefEvent]
public readonly record struct TreatWoundEvent;
