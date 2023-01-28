using Content.Shared.Body.Part;
using Content.Shared.Medical.Wounds.Components;

namespace Content.Shared.Medical.Wounds.Systems;

[ByRefEvent]
public readonly record struct WoundableDestroyedEvent;

[ByRefEvent]
public readonly record struct WoundAddedEvent(EntityUid WoundableEntity,
    WoundableComponent Woundable, EntityUid WoundEntity, WoundComponent WoundComponent);

[ByRefEvent]
public readonly record struct BodyWoundAddedEvent(EntityUid BodyEntity, EntityUid BodyPartEntity,
    WoundableComponent Woundable, EntityUid WoundEntity, BodyPartComponent BodyPart, WoundComponent WoundComponent);

[ByRefEvent]
public readonly record struct WoundRemovedEvent(EntityUid Target,
    WoundableComponent Woundable, EntityUid WoundEntity, WoundComponent WoundComponent);

[ByRefEvent]
public readonly record struct BodyWoundRemovedEvent(EntityUid BodyEntity, EntityUid BodyPartEntity,
    WoundableComponent Woundable, EntityUid WoundEntity, WoundComponent WoundComponent);

[ByRefEvent]
public readonly record struct WoundSeverityChangedEvent(EntityUid Target, EntityUid WoundEntity,
    WoundComponent WoundComponent);

[ByRefEvent]
public readonly record struct BodyWoundSeverityChangedEvent(EntityUid BodyEntity, EntityUid BodyPartEntity,
    EntityUid WoundEntity, WoundComponent WoundComponent);
