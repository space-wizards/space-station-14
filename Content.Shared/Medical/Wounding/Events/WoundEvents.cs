using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounding.Components;


namespace Content.Shared.Medical.Wounding.Events;

[ByRefEvent]
public record struct CreateWoundAttemptEvent(
    Entity<WoundableComponent> TargetWoundable,
    Entity<WoundComponent> PossibleWound,
    bool Canceled = false);

[ByRefEvent]
public record struct WoundAppliedEvent(
    Entity<WoundableComponent> ParentWoundable,
    Entity<WoundComponent> Wound);

[ByRefEvent]
public record struct WoundRemovedEvent(
    Entity<WoundableComponent> ParentWoundable,
    Entity<WoundComponent> Wound);

[ByRefEvent]
public record struct RemoveWoundAttemptEvent(
    Entity<WoundableComponent> TargetWoundable,
    Entity<WoundComponent> WoundToRemove,
    bool CancelRemove = false);


[ByRefEvent]
public record struct WoundFullyHealedEvent(
    Entity<WoundableComponent> ParentWoundable,
    Entity<WoundComponent> Wound);

[ByRefEvent]
public record struct WoundHealAttemptEvent(Entity<WoundComponent, HealableComponent> TargetWound, bool Canceled = false);

[ByRefEvent]
public record struct WoundSeverityChangedEvent(Entity<WoundComponent> TargetWound, FixedPoint2 SeverityDelta);
