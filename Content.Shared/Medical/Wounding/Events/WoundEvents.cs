using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounding.Components;
using Robust.Shared.Prototypes;

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
public record struct WoundableHealAttemptEvent(Entity<WoundableComponent, HealableComponent> TargetWoundable, bool Canceled = false);

[ByRefEvent]
public record struct WoundableHealUpdateEvent(Entity<WoundableComponent, HealableComponent> TargetWoundable, FixedPoint2 OldHealth);

[ByRefEvent]
public record struct WoundHealAttemptEvent(Entity<WoundComponent, HealableComponent> TargetWound, bool Canceled = false);

[ByRefEvent]
public record struct WoundHealUpdateEvent(Entity<WoundComponent, HealableComponent> TargetWound, FixedPoint2 OldSeverity);
