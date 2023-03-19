using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounds.Components;

namespace Content.Shared.Medical.Wounds.Systems;

[ByRefEvent]
public readonly record struct WoundableDestroyedEvent;

[ByRefEvent]
public readonly record struct WoundAddedEvent(EntityUid WoundableEntity, EntityUid WoundEntity,
    WoundableComponent Woundable, WoundComponent WoundComponent);

[ByRefEvent]
public readonly record struct WoundRemovedEvent(EntityUid WoundableEntity, EntityUid WoundEntity,
    WoundableComponent Woundable, WoundComponent WoundComponent);

[ByRefEvent]
public readonly record struct WoundSeverityChangedEvent(EntityUid WoundableEntity, EntityUid WoundEntity,
    WoundComponent WoundComponent, FixedPoint2 OldSeverity);

[ByRefEvent]
public readonly record struct WoundCauterizedEvent(EntityUid WoundableEntity, EntityUid WoundEntity,
    WoundableComponent Woundable, WoundComponent WoundComponent, bool OldState);
