using Content.Shared.Medical.Wounds.Components;

namespace Content.Shared.Medical.Wounds.Systems;

[ByRefEvent]
public readonly record struct WoundableDestroyedEvent;

[ByRefEvent]
public readonly record struct WoundAddedEvent(EntityUid WoundableEntity,
    WoundableComponent Woundable, EntityUid WoundEntity, WoundComponent WoundComponent);

[ByRefEvent]
public readonly record struct WoundRemovedEvent(EntityUid Target,
    WoundableComponent Woundable, EntityUid WoundEntity, WoundComponent WoundComponent);

[ByRefEvent]
public readonly record struct WoundSeverityChangedEvent(EntityUid Target, EntityUid WoundEntity,
    WoundComponent WoundComponent);
