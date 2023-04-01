using Content.Shared.Medical.Wounds.Components;

namespace Content.Shared.Medical.Treatments.Systems;

[ByRefEvent]
public readonly record struct WoundTreatedEvent(EntityUid WoundId, WoundComponent Wound);

[ByRefEvent]
public readonly record struct TreatWoundEvent;
