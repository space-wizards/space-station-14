using Content.Shared.Body.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounding.Components;


namespace Content.Shared.Medical.Wounding.Events;

[ByRefEvent]
public record struct CreateWoundAttemptEvent(
    Entity<WoundableComponent> TargetWoundable,
    Entity<WoundComponent> PossibleWound,
    bool Canceled = false);

[ByRefEvent]
public record struct WoundCreatedEvent(
    Entity<WoundableComponent> ParentWoundable,
    Entity<WoundComponent> Wound);


[ByRefEvent]
public record struct DestroyWoundAttemptEvent(
    Entity<WoundableComponent> TargetWoundable,
    Entity<WoundComponent> WoundToRemove,
    bool CancelRemove = false);

[ByRefEvent]
public record struct WoundDestroyedEvent(
    Entity<WoundableComponent> ParentWoundable,
    Entity<WoundComponent> Wound);

[ByRefEvent]
public record struct SetWoundSeverityAttemptEvent(
    Entity<WoundComponent> TargetWound,
    FixedPoint2 NewSeverity,
    bool Cancel = false);

[ByRefEvent]
public record struct WoundSeverityChangedEvent(
    Entity<WoundComponent> TargetWound,
    FixedPoint2 PreviousSeverity);

[ByRefEvent]
public record struct WoundAppliedToBody(
    Entity<BodyComponent> Body,
    Entity<WoundableComponent> Woundable,
    Entity<WoundComponent> Wound);

[ByRefEvent]
public record struct WoundRemovedFromBody(
    Entity<BodyComponent> Body,
    Entity<WoundableComponent> Woundable,
    Entity<WoundComponent> Wound);
